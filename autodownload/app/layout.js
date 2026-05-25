import "./globals.css";

export const metadata = {
  title: "AutoDownload",
  description: "Automação de download de boletos recorrentes",
};

export default function RootLayout({ children }) {
  return (
    <html lang="pt-BR" suppressHydrationWarning>
      <body>
        <script
          dangerouslySetInnerHTML={{
            __html:
              "try{var t=localStorage.getItem('autodownload.theme');document.documentElement.dataset.theme=t==='dark'?'dark':'light'}catch(e){}",
          }}
        />
        {children}
      </body>
    </html>
  );
}
