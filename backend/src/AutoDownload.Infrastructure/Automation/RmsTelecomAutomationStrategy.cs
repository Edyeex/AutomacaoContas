using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoDownload.Application.Abstractions;
using AutoDownload.Domain.Entities;
using AutoDownload.Domain.Enums;
using AutoDownload.Domain.ValueObjects;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace AutoDownload.Infrastructure.Automation;

internal sealed class RmsTelecomAutomationOptions
{
    public string LoginUrl { get; init; } = "https://fatura.rmstelecom.net/login";

    public string PortalUrl { get; init; } = "https://fatura.rmstelecom.net/";

    public string InvoicesUrl { get; init; } = "https://fatura.rmstelecom.net/invoices";

    public string ApiBaseUrl { get; init; } = "https://api.portal.cs30.7az.com.br/";

    public string DownloadDirectory { get; init; } = "%USERPROFILE%\\Downloads";

    public bool Headless { get; init; }

    public int TimeoutSeconds { get; init; } = 30;

    public int DownloadTimeoutSeconds { get; init; } = 60;

    public int PaymentWindowDays { get; init; } = 31;
}

internal sealed class RmsTelecomAutomationStrategy : IOperatorAutomationStrategy
{
    public const string OperatorCode = "rms-telecom";

    private static readonly HttpClient PortalHttpClient = new();

    private readonly RmsTelecomAutomationOptions options;

    public RmsTelecomAutomationStrategy(IOptions<RmsTelecomAutomationOptions> options)
    {
        this.options = options.Value;
    }

    public bool CanHandle(OperatorCompany operatorCompany)
        => operatorCompany.IsActive && operatorCompany.Code == OperatorCode;

    public Task<AutomationDownloadResult> DownloadCurrentBillAsync(
        AutomationExecutionContext context,
        CancellationToken cancellationToken = default)
        => Task.Run(() => DownloadCurrentBill(context, cancellationToken), cancellationToken);

    private AutomationDownloadResult DownloadCurrentBill(
        AutomationExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(context.Credential.Login) ||
            string.IsNullOrWhiteSpace(context.Credential.Password))
        {
            return new AutomationDownloadResult(
                AutomationRunStatus.LoginFailed,
                "Credenciais do portal RMS estao incompletas.",
                null);
        }

        var downloadRoot = ResolveDownloadRoot(options.DownloadDirectory);
        var tempDirectory = Path.Combine(Path.GetTempPath(), "AutoDownload", "rms", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            using var driver = CreateDriver(tempDirectory);
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(Math.Max(5, options.TimeoutSeconds)));

            Login(driver, wait, context.Credential, cancellationToken);
            OpenInvoicesArea(driver, wait, cancellationToken);

            var downloadedInvoice = DownloadPayableInvoice(driver, wait, tempDirectory, cancellationToken);
            var reference = BillReference.FromDate(context.ReferenceDate);
            var fileName = BuildFileName(context);
            Directory.CreateDirectory(downloadRoot);

            var finalPath = Path.Combine(downloadRoot, fileName);
            File.Move(downloadedInvoice.FilePath, finalPath, overwrite: true);

            return new AutomationDownloadResult(
                AutomationRunStatus.Success,
                $"Fatura RMS de {reference} baixada com sucesso.",
                new BillDraft(
                    reference,
                    downloadedInvoice.DueDate ?? BuildDueDate(context.ReferenceDate),
                    downloadedInvoice.Amount ?? 0m,
                    fileName,
                    finalPath));
        }
        catch (PortalLoginFailedException ex)
        {
            return new AutomationDownloadResult(AutomationRunStatus.LoginFailed, ex.Message, null);
        }
        catch (WebDriverTimeoutException)
        {
            return new AutomationDownloadResult(
                AutomationRunStatus.BillUnavailable,
                "Nao foi possivel localizar boleto RMS disponivel para download.",
                null);
        }
        catch (NoSuchElementException)
        {
            return new AutomationDownloadResult(
                AutomationRunStatus.BillUnavailable,
                "Nao foi possivel localizar os controles de boleto no portal RMS.",
                null);
        }
        catch (WebDriverException ex)
        {
            return new AutomationDownloadResult(
                AutomationRunStatus.ConnectionError,
                $"Falha ao automatizar o portal RMS: {ex.Message}",
                null);
        }
        finally
        {
            TryDeleteDirectory(tempDirectory);
        }
    }

    private IWebDriver CreateDriver(string downloadDirectory)
    {
        var chromeOptions = new ChromeOptions();
        var profileDirectory = Path.Combine(downloadDirectory, "chrome-profile");
        Directory.CreateDirectory(profileDirectory);

        chromeOptions.AddUserProfilePreference("download.default_directory", downloadDirectory);
        chromeOptions.AddUserProfilePreference("download.prompt_for_download", false);
        chromeOptions.AddUserProfilePreference("download.directory_upgrade", true);
        chromeOptions.AddUserProfilePreference("plugins.always_open_pdf_externally", true);
        chromeOptions.AddArgument("--disable-blink-features=AutomationControlled");
        chromeOptions.AddArgument("--disable-extensions");
        chromeOptions.AddArgument("--disable-gpu");
        chromeOptions.AddArgument("--disable-popup-blocking");
        chromeOptions.AddArgument("--remote-debugging-port=0");
        chromeOptions.AddArgument("--start-maximized");
        chromeOptions.AddArgument($"--user-data-dir={profileDirectory}");
        chromeOptions.AddExcludedArgument("enable-automation");

        if (OperatingSystem.IsLinux())
        {
            chromeOptions.BinaryLocation = ResolveLinuxChromiumBinary();
            chromeOptions.AddArgument("--no-sandbox");
            chromeOptions.AddArgument("--disable-dev-shm-usage");
            chromeOptions.AddArgument("--disable-setuid-sandbox");
        }

        if (options.Headless)
        {
            chromeOptions.AddArgument("--headless=new");
            chromeOptions.AddArgument("--window-size=1366,900");
        }

        var service = CreateDriverService();
        service.HideCommandPromptWindow = true;
        service.EnableVerboseLogging = true;

        var driver = new ChromeDriver(service, chromeOptions);
        ((IJavaScriptExecutor)driver)
            .ExecuteScript("Object.defineProperty(navigator, 'webdriver', {get: () => undefined})");

        return driver;
    }

    private static ChromeDriverService CreateDriverService()
    {
        if (!OperatingSystem.IsLinux())
        {
            return ChromeDriverService.CreateDefaultService();
        }

        var driverPath = TryResolveExistingFile(
            "/usr/bin/chromedriver",
            "/usr/local/bin/chromedriver",
            "/usr/lib/chromium/chromedriver");

        if (driverPath is null)
        {
            return ChromeDriverService.CreateDefaultService();
        }

        return ChromeDriverService.CreateDefaultService(
            Path.GetDirectoryName(driverPath),
            Path.GetFileName(driverPath));
    }

    private static string ResolveLinuxChromiumBinary()
        => ResolveExistingFile(
            "Chromium binary",
            "/usr/bin/chromium",
            "/usr/bin/chromium-browser",
            "/usr/bin/google-chrome",
            "/opt/google/chrome/chrome");

    private static string ResolveExistingFile(string description, params string[] candidates)
        => TryResolveExistingFile(candidates)
           ?? throw new WebDriverException($"{description} was not found in the container.");

    private static string? TryResolveExistingFile(params string[] candidates)
        => candidates.FirstOrDefault(File.Exists);

    private void Login(
        IWebDriver driver,
        WebDriverWait wait,
        PortalCredential credential,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        driver.Navigate().GoToUrl(options.LoginUrl);
        WaitForDocument(driver, wait);

        var documentInput = wait.Until(current =>
        {
            var candidate = current.FindElement(By.CssSelector("#cpfcnpj"));
            return candidate.Displayed && candidate.Enabled ? candidate : null;
        });
        documentInput.Clear();
        documentInput.SendKeys(credential.Login);

        var passwordInput = wait.Until(current =>
        {
            var candidate = current.FindElement(By.CssSelector("#passwd"));
            return candidate.Displayed && candidate.Enabled ? candidate : null;
        });
        passwordInput.Clear();
        passwordInput.SendKeys(credential.Password);

        var loginButton = wait.Until(current =>
        {
            var candidate = current.FindElement(By.CssSelector("#loginButton"));
            return candidate.Displayed && candidate.Enabled ? candidate : null;
        });
        ClickElement(driver, loginButton);

        wait.Until(current =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var body = SafeBodyText(current);
            if (LooksLikeLoginError(body))
            {
                throw new PortalLoginFailedException("Falha de login no portal RMS. Confira CPF e senha.");
            }

            return !IsLoginPage(current);
        });
    }

    private void OpenInvoicesArea(
        IWebDriver driver,
        WebDriverWait wait,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        driver.Navigate().GoToUrl(options.InvoicesUrl);
        WaitForDocument(driver, wait);

        wait.Until(current =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (LooksLikeLoginError(SafeBodyText(current)))
            {
                throw new PortalLoginFailedException("Falha de login no portal RMS. Confira CPF e senha.");
            }

            return !IsLoginPage(current) &&
                   (!string.IsNullOrWhiteSpace(GetPortalToken(current)) || PageLooksLikeBillingArea(current));
        });
    }

    private RmsInvoiceDownload DownloadPayableInvoice(
        IWebDriver driver,
        WebDriverWait wait,
        string tempDirectory,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var apiDownload = TryDownloadPayableInvoiceFromApi(driver, tempDirectory, cancellationToken);
        if (apiDownload is not null)
        {
            return apiDownload;
        }

        return DownloadPayableInvoiceFromPage(driver, wait, tempDirectory, cancellationToken);
    }

    private RmsInvoiceDownload? TryDownloadPayableInvoiceFromApi(
        IWebDriver driver,
        string tempDirectory,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var token = GetPortalToken(driver);
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        try
        {
            var invoice = GetPayableInvoiceFromApi(token, cancellationToken);
            invoice = GetInvoicePaymentDataFromApi(invoice, token, cancellationToken);
            var pdfBytes = DownloadInvoicePdfFromApi(invoice.Id, token, cancellationToken);
            var pdfPath = Path.Combine(tempDirectory, $"rms_invoice_{SanitizeFileName(invoice.Id)}.pdf");

            File.WriteAllBytes(pdfPath, pdfBytes);

            return new RmsInvoiceDownload(pdfPath, invoice.DueDate, invoice.Amount);
        }
        catch (WebDriverTimeoutException)
        {
            return null;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private RmsInvoice GetPayableInvoiceFromApi(string token, CancellationToken cancellationToken)
    {
        using var request = CreatePortalApiRequest(HttpMethod.Get, "invoices", token);
        using var response = PortalHttpClient.Send(request, cancellationToken);
        var body = response.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Portal RMS retornou {(int)response.StatusCode} ao consultar faturas.");
        }

        var invoices = ParseInvoices(body);
        if (invoices.Count == 0)
        {
            throw new HttpRequestException("Portal RMS nao retornou faturas para selecao.");
        }

        var paymentLimit = DateOnly
            .FromDateTime(DateTime.Now)
            .AddDays(Math.Max(1, options.PaymentWindowDays));
        var payableInvoice = invoices
            .Where(invoice => IsPayableInvoice(invoice, paymentLimit))
            .OrderBy(invoice => invoice.DueDate ?? DateOnly.MaxValue)
            .FirstOrDefault();

        if (payableInvoice is null)
        {
            throw new WebDriverTimeoutException("Nao ha fatura RMS disponivel para pagamento.");
        }

        return payableInvoice;
    }

    private RmsInvoice GetInvoicePaymentDataFromApi(
        RmsInvoice invoice,
        string token,
        CancellationToken cancellationToken)
    {
        using var request = CreatePortalApiRequest(
            HttpMethod.Get,
            $"invoices/{Uri.EscapeDataString(invoice.Id)}/payment-data",
            token);
        using var response = PortalHttpClient.Send(request, cancellationToken);
        var body = response.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();

        if (!response.IsSuccessStatusCode)
        {
            return invoice;
        }

        var details = ParseInvoiceDetails(body, invoice.Id);
        return new RmsInvoice(
            invoice.Id,
            details?.DueDate ?? invoice.DueDate,
            details?.Amount ?? invoice.Amount,
            details?.Paid ?? invoice.Paid);
    }

    private byte[] DownloadInvoicePdfFromApi(
        string invoiceId,
        string token,
        CancellationToken cancellationToken)
    {
        using var request = CreatePortalApiRequest(HttpMethod.Get, $"invoices/{Uri.EscapeDataString(invoiceId)}/pdf", token);
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));

        using var response = PortalHttpClient.Send(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Portal RMS retornou {(int)response.StatusCode} ao baixar a fatura.");
        }

        var pdfBytes = response.Content.ReadAsByteArrayAsync(cancellationToken).GetAwaiter().GetResult();
        if (!LooksLikePdf(pdfBytes))
        {
            throw new HttpRequestException("Portal RMS nao retornou um PDF valido para a fatura.");
        }

        return pdfBytes;
    }

    private HttpRequestMessage CreatePortalApiRequest(HttpMethod method, string relativePath, string token)
    {
        var baseUrl = options.ApiBaseUrl.EndsWith("/", StringComparison.Ordinal)
            ? options.ApiBaseUrl
            : options.ApiBaseUrl + "/";
        var portalOrigin = new Uri(options.PortalUrl).GetLeftPart(UriPartial.Authority);
        var request = new HttpRequestMessage(method, new Uri(new Uri(baseUrl), relativePath));

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Referrer = new Uri(options.PortalUrl);
        request.Headers.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/125.0 Safari/537.36");
        request.Headers.TryAddWithoutValidation("Origin", portalOrigin);

        return request;
    }

    private RmsInvoiceDownload DownloadPayableInvoiceFromPage(
        IWebDriver driver,
        WebDriverWait wait,
        string tempDirectory,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        OpenFirstPayableInvoice(driver, wait, cancellationToken);
        var detailsText = SafeBodyText(driver);
        ClickInvoicePdfAction(driver, wait, cancellationToken);
        var paymentText = SafeBodyText(driver);

        var pdf = TryWaitForPdfDownload(
            tempDirectory,
            TimeSpan.FromSeconds(Math.Min(10, Math.Max(5, options.DownloadTimeoutSeconds))),
            cancellationToken);
        var pageText = string.Join(Environment.NewLine, detailsText, paymentText, SafeBodyText(driver));
        pdf ??= CreatePaymentDataPdf(tempDirectory, pageText);

        return new RmsInvoiceDownload(pdf, ParseDueDate(pageText), ParseAmount(pageText));
    }

    private static void OpenFirstPayableInvoice(
        IWebDriver driver,
        WebDriverWait wait,
        CancellationToken cancellationToken)
    {
        var invoiceCard = wait.Until(current =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!DocumentIsReady(current))
            {
                return null;
            }

            var body = SafeBodyText(current);
            if (ContainsAny(body, "voce nao tem faturas pendentes", "nenhuma fatura", "nao existem faturas"))
            {
                throw new WebDriverTimeoutException("Nao ha fatura RMS disponivel para pagamento.");
            }

            var payNowAction = FindActionByText(current, "Pagar agora");
            if (payNowAction is not null)
            {
                return payNowAction;
            }

            var statusElements = current
                .FindElements(By.XPath("//*[contains(normalize-space(.), 'Vencida') or contains(normalize-space(.), 'A vencer')]"))
                .Where(IsVisibleAndEnabled)
                .ToList();

            foreach (var statusElement in statusElements)
            {
                var clickableCard = FindNearestClickableAncestor(current, statusElement);
                if (clickableCard is not null)
                {
                    return clickableCard;
                }
            }

            var payAction = FindActionByText(current, "Pagar");
            if (payAction is not null)
            {
                return payAction;
            }

            return null;
        });

        ScrollIntoView(driver, invoiceCard);
        ClickElement(driver, invoiceCard);
        wait.Until(current =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!DocumentIsReady(current))
            {
                return false;
            }

            return ContainsAny(SafeBodyText(current), "detalhes da fatura", "escolha como pagar", "vencimento");
        });
    }

    private static void ClickInvoicePdfAction(
        IWebDriver driver,
        WebDriverWait wait,
        CancellationToken cancellationToken)
    {
        var pdfAction = wait.Until(current =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!DocumentIsReady(current))
            {
                return null;
            }

            var action = FindActionByText(
                current,
                "Ver fatura",
                "Baixar fatura",
                "Download da fatura",
                "Visualizar fatura",
                "Boleto");
            if (action is not null)
            {
                return action;
            }

            var expandAction = FindActionByText(current, "Ver mais");
            if (expandAction is not null)
            {
                ScrollIntoView(current, expandAction);
                ClickElement(current, expandAction);
            }

            return FindActionByText(
                current,
                "Ver fatura",
                "Baixar fatura",
                "Download da fatura",
                "Visualizar fatura",
                "Boleto");
        });

        ScrollIntoView(driver, pdfAction);
        ClickElement(driver, pdfAction);
    }

    private static IWebElement? FindActionByText(IWebDriver driver, params string[] labels)
        => driver
            .FindElements(By.XPath("//*[self::button or self::a or @role='button' or contains(@class, 'cursor-pointer') or contains(@class, 'btn-base-submit')]"))
            .FirstOrDefault(element =>
            {
                if (!IsVisibleAndEnabled(element))
                {
                    return false;
                }

                return labels.Any(label => ContainsAny(element.Text, label));
            });

    private static IWebElement? FindNearestClickableAncestor(IWebDriver driver, IWebElement element)
    {
        const string script = """
            const element = arguments[0];
            let current = element;
            for (let i = 0; i < 8 && current; i += 1, current = current.parentElement) {
                const style = window.getComputedStyle(current);
                const tag = current.tagName ? current.tagName.toLowerCase() : '';
                const isClickable =
                    tag === 'button' ||
                    tag === 'a' ||
                    current.getAttribute('role') === 'button' ||
                    current.classList.contains('cursor-pointer') ||
                    style.cursor === 'pointer';

                if (isClickable && current.offsetParent !== null) {
                    return current;
                }
            }

            return null;
            """;

        return ((IJavaScriptExecutor)driver).ExecuteScript(script, element) as IWebElement;
    }

    private static bool PageLooksLikeBillingArea(IWebDriver driver)
    {
        var text = SafeBodyText(driver);
        return ContainsAny(text, "fatura", "boleto", "financeiro", "vencimento", "segunda via", "2a via");
    }

    private static bool IsLoginPage(IWebDriver driver)
        => driver.Url.Contains("/login", StringComparison.OrdinalIgnoreCase) ||
           driver.FindElements(By.CssSelector("#cpfcnpj")).Any();

    private static bool LooksLikeLoginError(string text)
        => ContainsAny(text, "senha invalida", "senha incorreta", "login invalido", "cpf invalido", "usuario nao encontrado", "credenciais");

    private static string? GetPortalToken(IWebDriver driver)
        => ((IJavaScriptExecutor)driver)
            .ExecuteScript("return window.localStorage.getItem('7azFrontToken');")
            ?.ToString();

    private static bool IsVisibleAndEnabled(IWebElement element)
    {
        try
        {
            return element.Displayed && element.Enabled;
        }
        catch (StaleElementReferenceException)
        {
            return false;
        }
    }

    private static bool ContainsAny(string? text, params string[] needles)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = Normalize(text);
        return needles.Any(needle => normalized.Contains(Normalize(needle), StringComparison.OrdinalIgnoreCase));
    }

    private static string Normalize(string value)
    {
        var formD = value.Normalize(NormalizationForm.FormD);
        var chars = formD
            .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            .ToArray();
        return new string(chars).Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    private static void WaitForDocument(IWebDriver driver, WebDriverWait wait)
        => wait.Until(DocumentIsReady);

    private static bool DocumentIsReady(IWebDriver driver)
        => ((IJavaScriptExecutor)driver)
            .ExecuteScript("return document.readyState")
            ?.ToString() is "complete" or "interactive";

    private static string? TryWaitForPdfDownload(
        string directory,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var hasPendingDownload = Directory
                .EnumerateFiles(directory, "*.crdownload", SearchOption.TopDirectoryOnly)
                .Any();
            var pdf = TryFindDownloadedPdf(directory);

            if (!hasPendingDownload && pdf is not null)
            {
                return pdf;
            }

            Thread.Sleep(TimeSpan.FromSeconds(1));
        }

        return null;
    }

    private static string? TryFindDownloadedPdf(string directory)
        => Directory
            .EnumerateFiles(directory, "*.pdf", SearchOption.TopDirectoryOnly)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();

    private static string CreatePaymentDataPdf(string directory, string pageText)
    {
        var path = Path.Combine(directory, $"rms_payment_data_{Guid.NewGuid():N}.pdf");
        var amount = ParseAmount(pageText);
        var dueDate = ParseDueDate(pageText);
        var barcode = TryParseBarcodeFromText(pageText) ?? "Codigo de barras nao identificado.";
        var amountText = amount?.ToString("C", CultureInfo.GetCultureInfo("pt-BR")) ?? "Nao identificado.";
        var dueDateText = dueDate?.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("pt-BR")) ?? "Nao identificado.";

        var lines = new[]
        {
            "AutoDownload - RMS Telecom",
            "Dados de pagamento do boleto",
            $"Valor: {amountText}",
            $"Vencimento: {dueDateText}",
            "Codigo de barras:",
            barcode,
            "",
            "Observacao:",
            "O portal RMS exibiu o codigo de barras, mas nao entregou um arquivo PDF para download.",
            "Este PDF foi gerado automaticamente pelo AutoDownload com os dados exibidos no portal."
        };

        File.WriteAllBytes(path, BuildSimplePdf(lines));
        return path;
    }

    private static string? TryParseBarcodeFromText(string text)
    {
        var formattedMatch = Regex.Match(
            text,
            @"\b\d{5}\.\d{5}\s+\d{5}\.\d{6}\s+\d{5}\.\d{6}\s+\d\s+\d{14}\b",
            RegexOptions.CultureInvariant);
        if (formattedMatch.Success)
        {
            return formattedMatch.Value;
        }

        var digitMatch = Regex.Match(text, @"\b\d{47,48}\b", RegexOptions.CultureInvariant);
        return digitMatch.Success ? digitMatch.Value : null;
    }

    private static byte[] BuildSimplePdf(IReadOnlyList<string> lines)
    {
        var contentBuilder = new StringBuilder();
        contentBuilder.AppendLine("BT");
        contentBuilder.AppendLine("/F1 16 Tf");
        contentBuilder.AppendLine("50 790 Td");

        var firstLine = true;
        foreach (var line in lines.SelectMany(WrapPdfLine))
        {
            if (!firstLine)
            {
                contentBuilder.AppendLine("0 -22 Td");
            }

            contentBuilder.Append('(');
            contentBuilder.Append(EscapePdfText(line));
            contentBuilder.AppendLine(") Tj");

            if (firstLine)
            {
                contentBuilder.AppendLine("/F1 11 Tf");
                firstLine = false;
            }
        }

        contentBuilder.AppendLine("ET");
        var content = contentBuilder.ToString();
        var contentLength = Encoding.ASCII.GetByteCount(content);

        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {contentLength} >>\nstream\n{content}endstream"
        };

        using var stream = new MemoryStream();
        WriteAscii(stream, "%PDF-1.4\n");

        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Length; index++)
        {
            offsets.Add(stream.Position);
            WriteAscii(stream, $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }

        var xrefOffset = stream.Position;
        WriteAscii(stream, $"xref\n0 {objects.Length + 1}\n");
        WriteAscii(stream, "0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            WriteAscii(stream, $"{offset:0000000000} 00000 n \n");
        }

        WriteAscii(
            stream,
            $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");

        return stream.ToArray();
    }

    private static IEnumerable<string> WrapPdfLine(string line)
    {
        const int maxLength = 86;
        if (line.Length <= maxLength)
        {
            yield return line;
            yield break;
        }

        for (var index = 0; index < line.Length; index += maxLength)
        {
            yield return line.Substring(index, Math.Min(maxLength, line.Length - index));
        }
    }

    private static string EscapePdfText(string value)
        => value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);

    private static void WriteAscii(Stream stream, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static IReadOnlyList<RmsInvoice> ParseInvoices(string json)
    {
        using var document = JsonDocument.Parse(json);
        var invoiceNodes = EnumerateInvoiceNodes(document.RootElement);

        return invoiceNodes
            .Select(ParseInvoice)
            .Where(invoice => invoice is not null)
            .Select(invoice => invoice!)
            .ToList();
    }

    private static IEnumerable<JsonElement> EnumerateInvoiceNodes(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            return root.EnumerateArray().ToList();
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            return Array.Empty<JsonElement>();
        }

        foreach (var propertyName in new[] { "data", "invoices", "items", "results" })
        {
            if (BillMetadataExtractor.TryGetProperty(root, propertyName, out var property) &&
                property.ValueKind == JsonValueKind.Array)
            {
                return property.EnumerateArray().ToList();
            }
        }

        return Array.Empty<JsonElement>();
    }

    private static RmsInvoice? ParseInvoice(JsonElement element)
    {
        var id = GetJsonPropertyAsString(element, "id");
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var paid =
            GetJsonPropertyAsBoolean(element, "paid") == true ||
            GetJsonPropertyAsBoolean(element, "paidByPix") == true;
        var dueDate = BillMetadataExtractor.TryGetDueDateFromJson(element);
        var amount = BillMetadataExtractor.TryGetAmountFromJson(element);

        return new RmsInvoice(id, dueDate, amount, paid);
    }

    private static RmsInvoice? ParseInvoiceDetails(string json, string fallbackId)
    {
        using var document = JsonDocument.Parse(json);

        foreach (var candidate in EnumerateDetailCandidates(document.RootElement))
        {
            var amount = BillMetadataExtractor.TryGetAmountFromJson(candidate);
            var dueDate = BillMetadataExtractor.TryGetDueDateFromJson(candidate);
            var paid =
                GetJsonPropertyAsBoolean(candidate, "paid") == true ||
                GetJsonPropertyAsBoolean(candidate, "paidByPix") == true;

            if (amount is not null || dueDate is not null)
            {
                return new RmsInvoice(fallbackId, dueDate, amount, paid);
            }
        }

        return null;
    }

    private static IEnumerable<JsonElement> EnumerateDetailCandidates(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        yield return root;

        foreach (var propertyName in new[] { "data", "invoice", "invoiceData", "paymentData", "details" })
        {
            if (BillMetadataExtractor.TryGetProperty(root, propertyName, out var property) &&
                property.ValueKind == JsonValueKind.Object)
            {
                yield return property;
            }
        }
    }

    private static bool IsPayableInvoice(RmsInvoice invoice, DateOnly paymentLimit)
    {
        if (invoice.Paid)
        {
            return false;
        }

        return invoice.DueDate is null || invoice.DueDate <= paymentLimit;
    }

    private static string? GetJsonPropertyAsString(JsonElement element, string propertyName)
    {
        if (!BillMetadataExtractor.TryGetProperty(element, propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };
    }

    private static bool? GetJsonPropertyAsBoolean(JsonElement element, string propertyName)
    {
        if (!BillMetadataExtractor.TryGetProperty(element, propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(value.GetString(), out var parsedBoolean) => parsedBoolean,
            JsonValueKind.Number when value.TryGetInt32(out var parsedInteger) => parsedInteger != 0,
            _ => null
        };
    }

    private static void ClickElement(IWebDriver driver, IWebElement element)
    {
        try
        {
            element.Click();
        }
        catch (ElementClickInterceptedException)
        {
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", element);
        }
    }

    private static void ScrollIntoView(IWebDriver driver, IWebElement element)
        => ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({ block: 'center' });", element);

    private static string SafeBodyText(IWebDriver driver)
    {
        try
        {
            return driver.FindElement(By.TagName("body")).Text;
        }
        catch (NoSuchElementException)
        {
            return string.Empty;
        }
    }

    private static decimal? ParseAmount(string text)
        => BillMetadataExtractor.TryParseAmountFromText(text);

    private static DateOnly? ParseDueDate(string text)
        => BillMetadataExtractor.TryParseDueDateFromText(text);

    private static string BuildFileName(AutomationExecutionContext context)
        => $"rms_{context.ReferenceDate:yyyy_MM}_{context.Account.Id:N}.pdf";

    private static DateOnly BuildDueDate(DateOnly referenceDate)
        => new(referenceDate.Year, referenceDate.Month, 20);

    private static string ResolveDownloadRoot(string configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? "%USERPROFILE%\\Downloads"
            : configuredPath;

        return Path.GetFullPath(Environment.ExpandEnvironmentVariables(path));
    }

    private static bool LooksLikePdf(byte[] bytes)
    {
        if (bytes.Length < 4)
        {
            return false;
        }

        var header = Encoding.ASCII.GetString(bytes.AsSpan(0, Math.Min(bytes.Length, 8)));
        return header.Contains("%PDF", StringComparison.Ordinal);
    }

    private static string SanitizeFileName(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new string(value
            .Select(character => invalidCharacters.Contains(character) ? '_' : character)
            .ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? Guid.NewGuid().ToString("N") : sanitized;
    }

    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed class PortalLoginFailedException : Exception
    {
        public PortalLoginFailedException(string message)
            : base(message)
        {
        }
    }

    private sealed record RmsInvoice(string Id, DateOnly? DueDate, decimal? Amount, bool Paid);

    private sealed record RmsInvoiceDownload(string FilePath, DateOnly? DueDate, decimal? Amount);
}
