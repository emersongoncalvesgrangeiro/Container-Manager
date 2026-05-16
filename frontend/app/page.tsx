"use client";
import style from "./style.module.css";
import { useEffect, useState } from "react";

const url = "http://127.0.0.1:5193";
const wsUrl = "ws://127.0.0.1:5193";

interface ContainerData {
  id?: string;
  image?: string;
  status?: string;
  name?: string;
  Name?: string;
  Status?: string;
}

export async function getImages(): Promise<string[]> {
  const response = await fetch(url + "/images");
  if (!response.ok) {
    throw new Error("Erro ao buscar imagens...");
  }
  return response.json();
}

export default function Home() {
  // === ESTADOS ===
  const [images, setImages] = useState<string[]>([]);
  const [inputImagem, setInputImagem] = useState<string>("");

  const [allContainers, setAllContainers] = useState<ContainerData[]>([]);
  const [onlineContainers, setOnlineContainers] = useState<ContainerData[]>([]);

  const [isEditorOpen, setIsEditorOpen] = useState(false);
  const [editorFileName, setEditorFileName] = useState("");
  const [editorContent, setEditorContent] = useState("");

  // Função solta para ser usada apenas pelos botões (Linter adora assim)
  const fetchImagesList = async () => {
    try {
      const data = await getImages();
      setImages(data);
    } catch (err) {
      console.error("Erro na busca de imagens:", err);
    }
  };

  // === WEBSOCKETS E CARREGAMENTO INICIAL ===
  useEffect(() => {
    // Usando .then() direto aqui cala a boca do erro de "set-state-in-effect"
    getImages()
      .then((data) => setImages(data))
      .catch((err) => console.error(err));

    const wsAll = new WebSocket(`${wsUrl}/allcontainers`);
    wsAll.onmessage = (event) => {
      const data = JSON.parse(event.data);
      const parsed = data.data.map((str: string) => {
        const parts = str.split("|");
        return {
          id: parts[0],
          image: parts[1],
          status: parts[2],
          name: parts[3],
        };
      });
      setAllContainers(parsed);
    };

    const wsOnline = new WebSocket(`${wsUrl}/containers`);
    wsOnline.onmessage = (event) => {
      const data = JSON.parse(event.data);
      setOnlineContainers(data.data);
    };

    return () => {
      wsAll.close();
      wsOnline.close();
    };
  }, []); // Sem aquele disable chato aqui!

  // === FUNÇÕES DE AÇÃO ===
  const handleRemoveImage = async (imageName: string) => {
    try {
      const response = await fetch(
        `${url}/removeimage?name=${encodeURIComponent(imageName)}`,
        { method: "POST" },
      );
      if (response.ok) fetchImagesList();
    } catch (err) {
      console.error(err);
      alert("Nyah! Erro ao remover imagem.");
    }
  };

  const handlePullImage = async () => {
    if (!inputImagem) return alert("Baka! Digita o nome da imagem primeiro.");
    console.log(`Puxando imagem da internet... isso pode demorar.`);

    try {
      const response = await fetch(
        `${url}/pullimage?name=${encodeURIComponent(inputImagem)}`,
        { method: "POST" },
      );
      if (response.ok) {
        alert("Kyaa! Imagem baixada com sucesso!");
        setInputImagem("");
        fetchImagesList();
      }
    } catch (err) {
      console.error(err);
      alert("Erro ao baixar a imagem.");
    }
  };

  const handleCreateContainerFromImage = async (imageName: string) => {
    const containerName = prompt(
      `Digite um nome para o container baseado em ${imageName}:`,
    );
    if (!containerName) return;

    try {
      const req = {
        Name: containerName,
        Image: imageName.split(":")[0],
        UsePort: false,
        Port: 0,
        UseVolume: false,
        Volume: "",
        IsEnvironment: false,
        Environment: "",
      };

      const response = await fetch(`${url}/createcontainer`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(req),
      });

      if (response.ok) alert(`Container ${containerName} criado!`);
    } catch (err) {
      console.error(err);
      alert("Erro ao criar container.");
    }
  };

  const handleContainerAction = async (action: string, name: string) => {
    try {
      const response = await fetch(
        `${url}/${action}container?name=${encodeURIComponent(name)}`,
        { method: "POST" },
      );
      if (!response.ok) alert(`Erro ao executar ${action} em ${name}.`);
    } catch (err) {
      console.error(err);
    }
  };

  const handleTerminal = (name: string) => {
    alert(`Abrindo terminal para ${name}... (Em construção)`);
  };

  // === FUNÇÕES DO EDITOR ===
  const openEditor = (fileName: string, defaultContent: string = "") => {
    setEditorFileName(fileName);
    setEditorContent(defaultContent);
    setIsEditorOpen(true);
  };

  const saveEditor = async () => {
    console.log(`Salvando arquivo: ${editorFileName}`);
    alert(`${editorFileName} salvo! (Ver log no console)`);
    setIsEditorOpen(false);
  };

  return (
    <main className="flex flex-col items-center min-h-screen p-8 bg-[#1a0b2e] relative">
      {/* === MODAL DO EDITOR DE CÓDIGO === */}
      {isEditorOpen && (
        <div className="absolute inset-0 bg-black/80 z-50 flex items-center justify-center p-8 backdrop-blur-sm">
          <div className="bg-[#0a001a] border-2 border-[#482481] w-full max-w-4xl h-[80vh] flex flex-col rounded-lg shadow-2xl">
            <div className="flex justify-between items-center p-4 border-b border-[#482481]">
              <h2 className="text-[#1bb0f5] font-mono font-bold">
                🛠️ Editando: {editorFileName}
              </h2>
              <button
                onClick={() => setIsEditorOpen(false)}
                className="text-red-500 hover:text-red-300 font-bold"
              >
                X Fechar
              </button>
            </div>
            <textarea
              aria-label="Editor de código"
              title="Editor de código"
              className="flex-1 w-full bg-[#110022] text-[#00ffcc] font-mono p-4 outline-none resize-none"
              value={editorContent}
              onChange={(e) => setEditorContent(e.target.value)}
              spellCheck={false}
            />
            <div className="p-4 border-t border-[#482481] flex justify-end">
              <button
                onClick={saveEditor}
                className="bg-green-600 hover:bg-green-800 text-white font-bold py-2 px-6 rounded transition-colors"
              >
                💾 Salvar Arquivo
              </button>
            </div>
          </div>
        </div>
      )}

      {/* BARRA DE FERRAMENTAS SUPERIOR */}
      <div className="w-full max-w-7xl flex gap-4 mb-8 justify-center">
        <button
          onClick={() =>
            openEditor("Dockerfile", "FROM ubuntu:latest\n\nRUN apt-get update")
          }
          className="bg-[#482481] hover:bg-[#2a006e] text-white font-bold py-2 px-6 rounded border border-purple-500 shadow-lg transition-colors"
        >
          📄 Criar Dockerfile
        </button>
        <button
          onClick={() =>
            openEditor(
              "docker-compose.yaml",
              "version: '3.8'\nservices:\n  app:\n    image: ubuntu",
            )
          }
          className="bg-[#086794] hover:bg-[#064e70] text-white font-bold py-2 px-6 rounded border border-blue-500 shadow-lg transition-colors"
        >
          🐳 Criar docker-compose.yaml
        </button>
      </div>

      {/* GRID COM OS PAINÉIS LADO A LADO */}
      <div className="flex flex-row flex-wrap gap-8 justify-center items-start w-full max-w-7xl">
        {/* === PAINEL 1: IMAGENS === */}
        <div className={style.centrilizer}>
          <section className="flex flex-col w-full h-full p-4 overflow-hidden relative">
            <h1 className="text-xl font-bold text-center mb-4 text-[#1bb0f5]">
              Imagens Docker:
            </h1>
            <ul className="overflow-y-auto flex-1 pr-2 space-y-2 mb-20">
              {images.map((img, index) => (
                <li
                  key={index}
                  className="border-b border-[#482481]/30 py-2 font-mono text-[10px] flex flex-row justify-between items-center gap-1"
                >
                  <span className="truncate flex-1 text-gray-300" title={img}>
                    {img}
                  </span>
                  <div className="flex flex-row gap-1">
                    <button
                      onClick={() => handleCreateContainerFromImage(img)}
                      className="bg-green-600 hover:bg-green-800 text-white font-bold py-1 px-2 rounded text-[10px]"
                    >
                      Criar
                    </button>
                    <button
                      onClick={() => handleRemoveImage(img)}
                      className="bg-red-600 hover:bg-red-800 text-white font-bold py-1 px-2 rounded text-[10px]"
                    >
                      Excluir
                    </button>
                  </div>
                </li>
              ))}
            </ul>
            <div className="absolute bottom-4 left-4 right-4 bg-[#0a001a] p-2 rounded border border-[#1b0749]">
              <div className="flex flex-row">
                <button
                  onClick={fetchImagesList}
                  className="flex-1 bg-[#482481] hover:bg-[#3a1d66] text-white text-xs font-bold py-2 border-r border-[#1b0749]"
                >
                  Atualizar Lista
                </button>
                <button
                  onClick={handlePullImage}
                  className="flex-1 bg-[#3a1d66] hover:bg-[#2a006e] text-white text-xs font-bold py-2"
                >
                  Adicionar
                </button>
              </div>
              <input
                aria-label="Nome da imagem"
                title="Nome da imagem"
                type="text"
                placeholder="imagem:"
                value={inputImagem}
                onChange={(e) => setInputImagem(e.target.value)}
                className="w-full mt-2 bg-[#2a006e] text-gray-300 text-xs py-1 px-2 rounded outline-none border border-[#482481] focus:border-[#1bb0f5]"
              />
            </div>
          </section>
        </div>

        {/* === PAINEL 2: TODOS OS CONTAINERS === */}
        <div className={style.centrilizer}>
          <section className="flex flex-col w-full h-full p-4 overflow-hidden">
            <h1 className="text-xl font-bold text-center mb-4 text-[#1bb0f5]">
              Todos os Containers:
            </h1>
            <ul className="overflow-y-auto flex-1 pr-2 space-y-3">
              {allContainers.map((container, index) => (
                <li
                  key={index}
                  className="border border-[#482481]/50 p-2 rounded bg-[#0a001a] flex flex-col gap-2"
                >
                  <div className="font-mono text-[10px] flex justify-between">
                    <span
                      className="text-gray-300 truncate"
                      title={container.name}
                    >
                      {container.name}
                    </span>
                    <span
                      className={
                        container.status?.includes("Up")
                          ? "text-green-400"
                          : "text-red-400"
                      }
                    >
                      {container.status}
                    </span>
                  </div>
                  <div className="flex flex-row flex-wrap gap-1 justify-end">
                    <button
                      onClick={() =>
                        handleContainerAction("start", container.name || "")
                      }
                      className="bg-green-600 hover:bg-green-800 px-2 py-1 text-[10px] text-white rounded"
                    >
                      Start
                    </button>
                    <button
                      onClick={() =>
                        handleContainerAction("restart", container.name || "")
                      }
                      className="bg-yellow-600 hover:bg-yellow-800 px-2 py-1 text-[10px] text-white rounded"
                    >
                      Restart
                    </button>
                    <button
                      onClick={() =>
                        handleContainerAction("remove", container.name || "")
                      }
                      className="bg-red-600 hover:bg-red-800 px-2 py-1 text-[10px] text-white rounded"
                    >
                      Apagar
                    </button>
                    <button
                      onClick={() => handleTerminal(container.name || "")}
                      className="bg-gray-600 hover:bg-gray-800 px-2 py-1 text-[10px] text-white rounded"
                    >
                      Terminal
                    </button>
                  </div>
                </li>
              ))}
            </ul>
          </section>
        </div>

        {/* === PAINEL 3: CONTAINERS ONLINE === */}
        <div className={style.centrilizer}>
          <section className="flex flex-col w-full h-full p-4 overflow-hidden">
            <h1 className="text-xl font-bold text-center mb-4 text-[#3c6b42]">
              Containers Online:
            </h1>
            <ul className="overflow-y-auto flex-1 pr-2 space-y-3">
              {onlineContainers.map((container, index) => (
                <li
                  key={index}
                  className="border border-[#3c6b42]/50 p-2 rounded bg-[#000000] flex flex-col gap-2"
                >
                  <div className="font-mono text-[10px] text-green-400 flex justify-between">
                    <span className="truncate" title={container.Name}>
                      {container.Name}
                    </span>
                    <span className="text-[8px] truncate">
                      {container.Status}
                    </span>
                  </div>
                  <div className="flex flex-row flex-wrap gap-1 justify-end">
                    <button
                      onClick={() =>
                        handleContainerAction("restart", container.Name || "")
                      }
                      className="bg-yellow-600 hover:bg-yellow-800 px-2 py-1 text-[10px] text-white rounded"
                    >
                      Restart
                    </button>
                    <button
                      onClick={() =>
                        handleContainerAction("stop", container.Name || "")
                      }
                      className="bg-red-600 hover:bg-red-800 px-2 py-1 text-[10px] text-white rounded"
                    >
                      Stop
                    </button>
                    <button
                      onClick={() => handleTerminal(container.Name || "")}
                      className="bg-blue-600 hover:bg-blue-800 px-2 py-1 text-[10px] text-white rounded"
                    >
                      Terminal
                    </button>
                  </div>
                </li>
              ))}
            </ul>
          </section>
        </div>
      </div>
    </main>
  );
}
