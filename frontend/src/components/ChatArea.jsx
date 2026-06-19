import { useState, useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { chatApi } from '../services/api';

const Icons = {
    Send: () => <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M14 5l7 7m0 0l-7 7m7-7H3" /></svg>,
    Clip: () => <svg className="w-4 h-4 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15.172 7l-6.586 6.586a2 2 0 102.828 2.828l6.414-6.586a4 4 0 00-5.656-5.656l-6.415 6.585a6 6 0 108.486 8.486L20.5 13" /></svg>,
    Alert: () => <svg className="w-5 h-5 text-red-600 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" /></svg>,
    Edit: () => <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16.862 3.487a1.5 1.5 0 012.121 2.121l-9.192 9.192-2.12.707a.75.75 0 01-.936-.936l.707-2.12 9.192-9.192z" /></svg>
};

export default function ChatArea({ activeChat, onCreditsUpdated, selectedModel, onOpenRenameDialog }) {
    const [messages, setMessages] = useState([]);
    const [inputText, setInputText] = useState('');
    const [isStreaming, setIsStreaming] = useState(false);
    const [streamingText, setStreamingText] = useState('');
    const [hubConnection, setHubConnection] = useState(null);
    const [lowCreditsAlert, setLowCreditsAlert] = useState(false);
    
    const messagesEndRef = useRef(null);

    useEffect(() => {
        const token = localStorage.getItem('token');
        const connection = new signalR.HubConnectionBuilder()
            .withUrl('/api/service3/chat', { accessTokenFactory: () => token })
            .withAutomaticReconnect()
            .build();

        connection.start()
            .then(() => setHubConnection(connection))
            .catch(err => console.error('Error SignalR: ', err));

        connection.on("RecibirFragmento", (fragmento) => {
            setIsStreaming(true);
            setStreamingText(prev => prev + fragmento);
        });

        // ¡LA NUEVA LÓGICA! Espera paciente hasta que el backend diga "Terminé"
        connection.on("FinStream", (textoCompleto) => {
            setIsStreaming(false);
            setMessages(prev => [...prev, { idMensaje: Date.now(), rolActor: 'assistant', contenido: textoCompleto }]);
            setStreamingText('');
        });

        // Espera a que el backend emita el cobro final y actualiza la cabecera
        connection.on("ActualizarSaldo", (nuevoSaldo) => {
            if (onCreditsUpdated) {
                onCreditsUpdated(nuevoSaldo);
            }
        });

        return () => { connection.stop(); };
    }, []);

    useEffect(() => {
        if (!activeChat) return;
        const loadHistory = async () => {
            try {
                const res = await chatApi.get(`/conversaciones/${activeChat.idConversacion}/mensajes`);
                setMessages(res.data);
            } catch (err) { console.error("Error cargando historial", err); }
        };
        loadHistory();
        setStreamingText('');
        setIsStreaming(false);

        if (hubConnection?.state === signalR.HubConnectionState.Connected) {
            hubConnection.invoke("UnirseAChat", activeChat.idConversacion.toString());
        }
    }, [activeChat, hubConnection]);

    useEffect(() => {
        messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages, streamingText, isStreaming]);

    const handleSendMessage = async (e) => {
        e.preventDefault();
        if (!inputText.trim() || isStreaming || !activeChat) return;

        const currentText = inputText;
        setInputText('');
        setLowCreditsAlert(false);

        setMessages(prev => [...prev, { idMensaje: Date.now(), rolActor: 'user', contenido: currentText }]);

        try {
            // Pasamos la IA elegida
            const modeloId = selectedModel ? selectedModel.id : 'gemini';
            const res = await chatApi.post(`/conversaciones/${activeChat.idConversacion}/mensajes`, {
                contenido: currentText,
                modeloId: modeloId
            });
            // ¡Adiós setTimeout de 3 segundos! Todo fluye de forma natural ahora.

        } catch (error) {
            if (error.response && error.response.status === 400) {
                setLowCreditsAlert(true);
                setMessages(prev => prev.slice(0, -1)); 
            }
        }
    };

    if (!activeChat) return <div className="flex-1 flex items-center justify-center text-gray-500">Selecciona o crea un chat para comenzar.</div>;

    return (
        <div className="flex-1 flex flex-col h-full bg-white relative">
            <div className="h-12 border-b border-gray-100 px-4 flex items-center justify-between bg-white shrink-0">
                <div className="flex items-center gap-2 truncate">
                    <span className="text-gray-900 text-sm font-semibold truncate">{activeChat.tituloChat || activeChat.title}</span>
                    <button
                        type="button"
                        onClick={() => onOpenRenameDialog?.(activeChat.id || activeChat.idConversacion, activeChat.tituloChat || activeChat.title)}
                        className="text-gray-400 hover:text-blue-600 p-1.5 rounded transition-colors bg-white border border-transparent hover:border-gray-200"
                        title="Renombrar chat"
                    >
                        <Icons.Edit />
                    </button>
                </div>
            </div>

            {lowCreditsAlert && (
                <div className="m-4 p-3 bg-red-50 border border-red-200 rounded-md flex items-center justify-between shrink-0">
                    <div className="flex items-center">
                        <Icons.Alert />
                        <div className="text-sm text-red-800">
                            <span className="font-semibold mr-1">Créditos insuficientes.</span> 
                            <span>Recarga tu cuenta para usar la IA.</span>
                        </div>
                    </div>
                </div>
            )}

            <div className="flex-1 overflow-y-auto p-4 sm:p-6 space-y-6">
                {messages.length === 0 && !isStreaming ? (
                    <div className="h-full flex flex-col items-center justify-center text-center p-6">
                        <div className="bg-blue-50 text-blue-600 w-12 h-12 rounded-xl flex items-center justify-center mb-4 text-xl">✨</div>
                        <h3 className="text-base font-semibold text-gray-900">¿En qué puedo ayudarte hoy?</h3>
                    </div>
                ) : (
                    messages.map(m => {
                        const isUser = m.rolActor === 'user';
                        return (
                            <div key={m.idMensaje} className={`flex ${isUser ? 'justify-end' : 'justify-start'}`}>
                                <div className={`max-w-[85%] sm:max-w-[75%] rounded-2xl p-4 text-sm leading-relaxed ${isUser ? 'bg-blue-600 text-white rounded-br-sm' : 'bg-gray-50 border border-gray-100 text-gray-800 rounded-bl-sm'}`}>
                                    <div className="flex justify-between items-center mb-2">
                                        <span className={`text-[11px] font-semibold ${isUser ? 'text-blue-100' : 'text-gray-500'}`}>{isUser ? 'Tú' : 'Hub AI'}</span>
                                    </div>
                                    <p className="whitespace-pre-line">{m.contenido}</p>
                                </div>
                            </div>
                        );
                    })
                )}

                {isStreaming && (
                    <div className="flex justify-start">
                        <div className="max-w-[85%] sm:max-w-[75%] rounded-2xl rounded-bl-sm p-4 bg-gray-50 border border-gray-100 text-sm text-gray-800">
                            <span className="text-[11px] font-semibold text-gray-500 mb-2 block">Hub AI</span>
                            <p className="whitespace-pre-line">
                                <span>{streamingText}</span>
                                <span className="inline-block w-1.5 h-3 ml-1 bg-blue-500 animate-pulse"></span>
                            </p>
                        </div>
                    </div>
                )}
                <div ref={messagesEndRef} />
            </div>

            <div className="p-4 bg-white border-t border-gray-100 shrink-0">
                <form onSubmit={handleSendMessage} className="flex gap-2 max-w-4xl mx-auto relative">
                    <button 
                    type="button" 
                    onClick={() => alert("El microservicio de almacenamiento de archivos estará disponible en la Fase 2 del proyecto.")}
                    className="absolute left-3 top-2.5 p-1.5 text-gray-400 hover:text-gray-600 transition-colors bg-white">
                        <Icons.Clip />
                    </button>
                    <input 
                        type="text" value={inputText} onChange={(e) => setInputText(e.target.value)}
                        placeholder="Escribe un mensaje..." disabled={isStreaming}
                        className="flex-1 bg-gray-50 border border-gray-200 rounded-full pl-11 pr-16 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-blue-100"
                    />
                    <button type="submit" disabled={isStreaming || !inputText.trim()}
                        className={`absolute right-2 top-1.5 p-2 rounded-full flex items-center justify-center ${inputText.trim() && !isStreaming ? 'bg-blue-600 text-white shadow-sm' : 'bg-gray-100 text-gray-400 cursor-not-allowed'}`}>
                        <Icons.Send />
                    </button>
                </form>
            </div>
        </div>
    );
}