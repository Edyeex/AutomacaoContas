using AutoDownload.Application.Abstractions;
using AutoDownload.Domain.Entities;
using AutoDownload.Domain.Enums;
using AutoDownload.Domain.ValueObjects;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

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
        chromeOptions.AddUserProfilePreference("download.default_directory", downloadDirectory);
        chromeOptions.AddUserProfilePreference("download.prompt_for_download", false);
        chromeOptions.AddUserProfilePreference("download.directory_upgrade", true);
        chromeOptions.AddUserProfilePreference("plugins.always_open_pdf_externally", true);
        chromeOptions.AddArgument("--disable-blink-features=AutomationControlled");
        chromeOptions.AddArgument("--disable-popup-blocking");
        chromeOptions.AddArgument("--start-maximized");
        chromeOptions.AddExcludedArgument("enable-automation");

        if (options.Headless)
        {
            chromeOptions.AddArgument("--headless=new");
            chromeOptions.AddArgument("--window-size=1366,900");
        }

        var service = ChromeDriverService.CreateDefaultService();
        service.HideCommandPromptWindow = true;

        var driver = new ChromeDriver(service, chromeOptions);
        ((IJavaScriptExecutor)driver)
            .ExecuteScript("Object.defineProperty(navigator, 'webdriver', {get: () => undefined})");

        return driver;
    }

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
            current.FindElement(By.CssSelector("input[type='password']")));
        passwordInput.Clear();
        passwordInput.SendKeys(credential.Password);

        Click(wait, By.XPath("//button[contains(normalize-space(.), 'Entrar')]"), cancellationToken);

        wait.Until(current =>
            !current.Url.Contains("/login", StringComparison.OrdinalIgnoreCase) &&
            (current.Url.Contains("/minhavero", StringComparison.OrdinalIgnoreCase) ||
             current.Url.Contains("/dashboard", StringComparison.OrdinalIgnoreCase) ||
             current.Url.Contains("/painel", StringComparison.OrdinalIgnoreCase)));
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

        var element = wait.Until(current =>
            current.FindElement(By.XPath(
                "//*[contains(normalize-space(.), '2ª via da fatura') or contains(normalize-space(.), '2a via da fatura')]")));
        ScrollIntoView(driver, element);
        ClickElement(driver, element);
    }

    private static void ClickViewAndPrint(
        IWebDriver driver,
        WebDriverWait wait,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var element = wait.Until(current =>
            current.FindElement(By.XPath("//*[contains(normalize-space(.), 'Visualizar e Imprimir')]")));
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
}
