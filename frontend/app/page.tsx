"use client";
import style from "./style.module.css";
import { useEffect, useState } from "react";

const url = "http://localhost:5193";

export async function getImages(): Promise<string[]> {
  const response = await fetch(url + "/images");
  if (!response.ok) {
    throw new Error("Erro ao bucar imagens");
  }
  return response.json();
}

export default function Home() {
  const [images, setImages] = useState<string[]>([]);
  useEffect(() => {
    alert("Iniciando busca de imagens...");
    getImages()
      .then((data) => {
        setImages(data);
      })
      .catch((err) => {
        console.error(err);
      });
  }, []);
  const handleManualFetch = async () => {
    console.log("Botão clicado!");
    try {
      const data = await getImages();
      setImages(data);
    } catch (err) {
      alert("Erro ao buscar: " + err);
    }
  };
  return (
    <main className="flex flex-1 w-full max-w-3xl flex-col items-center justify-between py-32 px-16 sm:items-start">
      <div className="flex flex-row gap-4">
        <div className={style.centrilizer}>
          <section>
            <h1>Imagens:</h1>
            <button
              onClick={handleManualFetch}
              className="bg-blue-500 p-2 text-white"
            >
              FORÇAR BUSCA DE IMAGENS
            </button>
            <ul className="mt-4">
              {images.map((img, index) => (
                <li key={index} className="border-b py-1 font-mono text-sm">
                  {img}
                </li>
              ))}
            </ul>
          </section>
        </div>
      </div>
    </main>
  );
}
