// src/components/QRCodeDisplay.tsx
import React, { useEffect, useRef, useState } from "react";
import QRCode from "qrcode";
import { useLanguage } from "../LanguageContext";

type QRCodeDisplayProps = {
  data: string;
  itemName: string;
  sku?: string | null;
  serialNumber?: string | null;
  size?: number;
};

export default function QRCodeDisplay({
  data,
  itemName,
  sku,
  serialNumber,
  size = 256,
}: QRCodeDisplayProps) {
  const { t, language } = useLanguage();
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!canvasRef.current) return;

    QRCode.toCanvas(
      canvasRef.current,
      data,
      {
        width: size,
        margin: 2,
        color: {
          dark: "#000000",
          light: "#FFFFFF",
        },
      },
      (err) => {
        if (err) {
          console.error("QR Code generation failed:", err);
          setError(err.message);
        } else {
          setError(null);
        }
      }
    );
  }, [data, size]);

  const handlePrint = () => {
    const printWindow = window.open("", "_blank");
    if (!printWindow) {
      alert(language === "cs" ? "Nelze otevřít okno pro tisk" : "Cannot open print window");
      return;
    }

    const canvas = canvasRef.current;
    if (!canvas) return;

    const dataUrl = canvas.toDataURL("image/png");

    printWindow.document.write(`
      <!DOCTYPE html>
      <html>
        <head>
          <title>${language === "cs" ? "Tisk QR kódu" : "Print QR Code"} - ${itemName}</title>
          <style>
            @media print {
              @page {
                size: A4;
                margin: 20mm;
              }
            }
            body {
              font-family: Arial, sans-serif;
              display: flex;
              flex-direction: column;
              align-items: center;
              justify-content: center;
              min-height: 100vh;
              margin: 0;
              padding: 20px;
            }
            .qr-container {
              text-align: center;
              border: 2px solid #000;
              padding: 20px;
              border-radius: 8px;
            }
            .qr-title {
              font-size: 24px;
              font-weight: bold;
              margin-bottom: 10px;
            }
            .qr-info {
              font-size: 14px;
              color: #666;
              margin-bottom: 20px;
            }
            .qr-image {
              max-width: 300px;
              height: auto;
            }
          </style>
        </head>
        <body>
          <div class="qr-container">
            <div class="qr-title">${itemName}</div>
            <div class="qr-info">
              ${sku ? `SKU: ${sku}<br>` : ""}
              ${serialNumber ? `${language === "cs" ? "SN" : "Serial"}: ${serialNumber}` : ""}
            </div>
            <img src="${dataUrl}" alt="QR Code" class="qr-image" />
          </div>
          <script>
            window.onload = function() {
              window.print();
              window.onafterprint = function() {
                window.close();
              };
            };
          </script>
        </body>
      </html>
    `);
    printWindow.document.close();
  };

  if (error) {
    return (
      <div className="text-sm text-red-600">
        {language === "cs" ? "Chyba při generování QR kódu" : "QR Code generation error"}: {error}
      </div>
    );
  }

  return (
    <div className="space-y-3">
      <div className="flex justify-center">
        <canvas ref={canvasRef} className="border border-gray-200 rounded" />
      </div>
      
      <div className="flex justify-center">
        <button
          onClick={handlePrint}
          className="px-4 py-2 rounded-xl bg-blue-600 text-white hover:bg-blue-700 flex items-center gap-2"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            className="h-5 w-5"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z"
            />
          </svg>
          {t.printQr}
        </button>
      </div>

      <div className="text-xs text-gray-500 text-center">
        {language === "cs" 
          ? "Naskenuj tento kód pro rychlé zobrazení detailů položky" 
          : "Scan this code for quick item details"}
      </div>
    </div>
  );
}