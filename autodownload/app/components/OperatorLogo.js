"use client";

const OPERATOR_LOGOS = {
  "ceee-equatorial": "/operators/ceee-equatorial.png",
  "corsan": "/operators/corsan.png",
  "operador-demo": "/operators/operador-demo.png",
  "rms-telecom": "/operators/rms-telecom.png",
  "vero-internet": "/operators/vero-internet.png",
};

function normalize(value) {
  return String(value || "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase();
}

function operatorName(operator) {
  if (typeof operator === "string") return operator;
  return operator?.operadora || operator?.name || operator?.nome || "";
}

function operatorCode(operator) {
  const code = normalize(operator?.code || operator?.codigo || "");
  const name = normalize(operatorName(operator));

  if (code === "operador-demo" || code === "operator-demo" || name.includes("demo")) {
    return "operador-demo";
  }

  if (code === "ceee-equatorial" || name.includes("ceee") || name.includes("equatorial")) {
    return "ceee-equatorial";
  }

  if (code === "corsan" || name.includes("corsan")) {
    return "corsan";
  }

  if (code === "rms-telecom" || name.includes("rms telecom") || name === "rms") {
    return "rms-telecom";
  }

  if (code === "vero-internet" || name.includes("vero")) {
    return "vero-internet";
  }

  return null;
}

export default function OperatorLogo({ operator, icon, className = "", size = "md" }) {
  const label = operatorName(operator) || "Operadora";
  const fallbackIcon = icon || operator?.icon || "";
  const logoSrc = OPERATOR_LOGOS[operatorCode(operator)];
  const classes = ["operator-logo", `operator-logo-${size}`, className].filter(Boolean).join(" ");

  if (logoSrc) {
    return (
      <span className={`${classes} operator-logo-image`} aria-label={label} title={label}>
        <img src={logoSrc} alt={label} />
      </span>
    );
  }

  return (
    <span className={classes} aria-label={label} title={label}>
      {fallbackIcon}
    </span>
  );
}
