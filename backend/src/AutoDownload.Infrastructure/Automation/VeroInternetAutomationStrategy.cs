using AutoDownload.Application.Abstractions;
using AutoDownload.Domain.Entities;
using AutoDownload.Domain.Enums;
using AutoDownload.Domain.ValueObjects;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Globalization;
using System.Text;

namespace AutoDownload.Infrastructure.Automation;

internal sealed class VeroInternetAutomationOptions
{
    public string LoginUrl { get; init; } = "https://verointernet.com.br/minhavero/login";

    public string InvoiceUrl { get; init; } = "https://verointernet.com.br/minhavero/fatura";

    public string DownloadDirectory { get; init; } = "%USERPROFILE%\\Downloads";

    public bool Headless { get; init; }

    public int TimeoutSeconds { get; init; } = 30;

    public int DownloadTimeoutSeconds { get; init; } = 60;
}

internal sealed class VeroInternetAutomationStrategy : IOperatorAutomationStrategy
{
    public const string OperatorCode = "vero-internet";

    private readonly VeroInternetAutomationOptions options;

    public VeroInternetAutomationStrategy(IOptions<VeroInternetAutomationOptions> options)
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
                "Credenciais do portal Vero estao incompletas.",
                null);
        }

        var downloadRoot = ResolveDownloadRoot(options.DownloadDirectory);
        var tempDirectory = Path.Combine(Path.GetTempPath(), "AutoDownload", "vero", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            using var driver = CreateDriver(tempDirectory);
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(Math.Max(5, options.TimeoutSeconds)));

            Login(driver, wait, context.Credential, cancellationToken);
            OpenInvoicePage(driver, wait, cancellationToken);
            var invoicePageText = SafeBodyText(driver);
            ClickSecondCopy(driver, wait, cancellationToken);
            var secondCopyText = SafeBodyText(driver);
            ClickViewAndPrint(driver, wait, cancellationToken);
            var printPageText = SafeBodyText(driver);

            var downloadedFile = WaitForPdfDownload(
                tempDirectory,
                TimeSpan.FromSeconds(Math.Max(10, options.DownloadTimeoutSeconds)),
                cancellationToken);

            var reference = BillReference.FromDate(context.ReferenceDate);
            var fileName = BuildFileName(context);
            var metadataText = string.Join(Environment.NewLine, invoicePageText, secondCopyText, printPageText);
            var amount = BillMetadataExtractor.TryParseAmountFromText(metadataText) ?? 0m;
            var dueDate = BillMetadataExtractor.TryParseDueDateFromText(metadataText) ?? BuildDueDate(context.ReferenceDate);
            Directory.CreateDirectory(downloadRoot);

            var finalPath = Path.Combine(downloadRoot, fileName);
            File.Move(downloadedFile, finalPath, overwrite: true);

            return new AutomationDownloadResult(
                AutomationRunStatus.Success,
                $"Fatura Vero de {reference} baixada com sucesso.",
                new BillDraft(
                    reference,
                    dueDate,
                    amount,
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
                "Nao foi possivel localizar a fatura Vero disponivel para download.",
                null);
        }
        catch (NoSuchElementException)
        {
            return new AutomationDownloadResult(
                AutomationRunStatus.BillUnavailable,
                "Nao foi possivel localizar os controles de fatura no portal Vero.",
                null);
        }
        catch (WebDriverException ex)
        {
            return new AutomationDownloadResult(
                AutomationRunStatus.ConnectionError,
                $"Falha ao automatizar o portal Vero: {ex.Message}",
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

        var documentInput = wait.Until(current =>
            current.FindElement(By.CssSelector("input[placeholder='Insira seu documento']")));
        documentInput.Clear();
        documentInput.SendKeys(credential.Login);

        Click(wait, By.CssSelector("button[type='submit']"), cancellationToken);

        var passwordInput = wait.Until(current =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var body = SafeBodyText(current);
            if (LooksLikeLoginError(body))
            {
                throw new PortalLoginFailedException("Portal Vero recusou o documento informado. Confira CPF/CNPJ ou login do portal.");
            }

            return current
                .FindElements(By.CssSelector("input[type='password']"))
                .FirstOrDefault(element => element.Displayed && element.Enabled);
        });
        passwordInput.Clear();
        passwordInput.SendKeys(credential.Password);

        Click(wait, By.XPath("//button[contains(normalize-space(.), 'Entrar')]"), cancellationToken);

        wait.Until(current =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var body = SafeBodyText(current);
            if (LooksLikeHumanVerification(body))
            {
                throw new PortalLoginFailedException(
                    "Portal Vero solicitou verificacao adicional. Acesse manualmente o portal e conclua a validacao antes de executar novamente.");
            }

            if (LooksLikeLoginError(body))
            {
                throw new PortalLoginFailedException("Portal Vero recusou o login. Confira CPF/documento e senha.");
            }

            return !current.Url.Contains("/login", StringComparison.OrdinalIgnoreCase) &&
                   (current.Url.Contains("/minhavero", StringComparison.OrdinalIgnoreCase) ||
                    current.Url.Contains("/dashboard", StringComparison.OrdinalIgnoreCase) ||
                    current.Url.Contains("/painel", StringComparison.OrdinalIgnoreCase));
        });
    }

    private void OpenInvoicePage(
        IWebDriver driver,
        WebDriverWait wait,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var invoiceLink = driver.FindElements(By.XPath("//a[contains(@href, 'fatura')]")).FirstOrDefault();
            if (invoiceLink is not null)
            {
                ClickElement(driver, invoiceLink);
                return;
            }
        }
        catch (WebDriverException)
        {
        }

        driver.Navigate().GoToUrl(options.InvoiceUrl);
        wait.Until(current => current.Url.Contains("fatura", StringComparison.OrdinalIgnoreCase));
    }

    private static void ClickSecondCopy(
        IWebDriver driver,
        WebDriverWait wait,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var element = wait.Until(current => FindVisibleElementByText(
            current,
            ["2", "via", "fatura"],
            ["2a", "via", "fatura"],
            ["segunda", "via", "fatura"]));
        ScrollIntoView(driver, element);
        ClickElement(driver, element);
    }

    private static void ClickViewAndPrint(
        IWebDriver driver,
        WebDriverWait wait,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var element = wait.Until(current => FindVisibleElementByText(
            current,
            ["visualizar", "imprimir"],
            ["ver", "imprimir"],
            ["imprimir"]));
        ScrollIntoView(driver, element);
        ClickElement(driver, element);
    }

    private static void Click(WebDriverWait wait, By by, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var element = wait.Until(current =>
        {
            var candidate = current.FindElement(by);
            return candidate.Displayed && candidate.Enabled ? candidate : null;
        });
        element.Click();
    }

    private static void ClickElement(IWebDriver driver, IWebElement element)
    {
        try
        {
            element.Click();
        }
        catch (WebDriverException)
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

    private static bool LooksLikeLoginError(string text)
        => ContainsAny(text, "senha invalida", "senha incorreta", "login invalido", "cpf/cnpj invalido", "cpf invalido", "documento invalido", "credenciais", "usuario nao encontrado");

    private static bool LooksLikeHumanVerification(string text)
        => ContainsAny(text, "captcha", "recaptcha", "verificacao", "validacao", "codigo de seguranca", "sms", "e-mail");

    private static IWebElement? FindVisibleElementByText(IWebDriver driver, params string[][] tokenSets)
    {
        foreach (var tokenSet in tokenSets)
        {
            var tokens = tokenSet.Select(NormalizeForSearch).Where(token => token.Length > 0).ToArray();
            var candidates = new List<(IWebElement Element, int Area)>();

            foreach (var element in driver.FindElements(By.XPath("//*[not(self::script) and not(self::style)]")))
            {
                try
                {
                    if (!element.Displayed)
                    {
                        continue;
                    }

                    var text = NormalizeForSearch(element.Text);
                    if (tokens.All(token => text.Contains(token, StringComparison.Ordinal)))
                    {
                        var clickableElement = FindClickableAncestor(driver, element);
                        candidates.Add((clickableElement, Math.Max(1, clickableElement.Size.Width * clickableElement.Size.Height)));
                    }
                }
                catch (WebDriverException)
                {
                }
            }

            var match = candidates.OrderBy(candidate => candidate.Area).FirstOrDefault();
            if (match.Element is not null)
            {
                return match.Element;
            }
        }

        return null;
    }

    private static IWebElement FindClickableAncestor(IWebDriver driver, IWebElement element)
    {
        try
        {
            return ((IJavaScriptExecutor)driver).ExecuteScript(
                """
                const element = arguments[0];

                function isClickable(node) {
                    if (!node || node.nodeType !== Node.ELEMENT_NODE) {
                        return false;
                    }

                    const tagName = node.tagName.toLowerCase();
                    const role = node.getAttribute('role');
                    const cursor = window.getComputedStyle(node).cursor;

                    return tagName === 'button'
                        || tagName === 'a'
                        || role === 'button'
                        || node.hasAttribute('onclick')
                        || cursor === 'pointer';
                }

                let current = element;
                while (current && current !== document.body) {
                    if (isClickable(current)) {
                        return current;
                    }

                    current = current.parentElement;
                }

                return element;
                """,
                element) as IWebElement ?? element;
        }
        catch (WebDriverException)
        {
            return element;
        }
    }

    private static bool ContainsAny(string? text, params string[] needles)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var haystack = NormalizeForSearch(text);

        return needles.Any(needle => haystack.Contains(NormalizeForSearch(needle), StringComparison.Ordinal));
    }

    private static string NormalizeForSearch(string? text)
    {
        var normalized = (text ?? string.Empty)
            .Normalize(NormalizationForm.FormKD)
            .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            .ToArray();

        return string.Join(
            ' ',
            new string(normalized)
                .Normalize(NormalizationForm.FormC)
                .ToLowerInvariant()
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static string WaitForPdfDownload(
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
            var pdf = Directory
                .EnumerateFiles(directory, "*.pdf", SearchOption.TopDirectoryOnly)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();

            if (!hasPendingDownload && pdf is not null)
            {
                return pdf;
            }

            Thread.Sleep(TimeSpan.FromSeconds(1));
        }

        throw new WebDriverTimeoutException("O download da fatura Vero nao foi concluido dentro do tempo esperado.");
    }

    private static string BuildFileName(AutomationExecutionContext context)
        => $"vero_{context.ReferenceDate:yyyy_MM}_{context.Account.Id:N}.pdf";

    private static DateOnly BuildDueDate(DateOnly referenceDate)
        => new(referenceDate.Year, referenceDate.Month, 20);

    private static string ResolveDownloadRoot(string configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? "%USERPROFILE%\\Downloads"
            : configuredPath;

        return Path.GetFullPath(Environment.ExpandEnvironmentVariables(path));
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
}
