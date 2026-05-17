import "./globals.css";

export const metadata = {
  title: "AutoDownload",
  description: "Automação de download de boletos recorrentes",
};

export default function RootLayout({ children }) {
  return (
    <html lang="pt-BR">
      <body>{children}</body>
    </html>
  );
}
