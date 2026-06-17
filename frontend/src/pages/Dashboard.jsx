import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { authApi, chatApi } from '../services/api';
import ChatArea from '../components/ChatArea';
import BillingArea from '../components/BillingArea';

// TUS ÍCONOS ORIGINALES RESTAURADOS
const Icons = {
    Plus: () => <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" /></svg>,
    Menu: () => <svg className="w-5 h-5 text-gray-600" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" /></svg>,
    ChevronDown: () => <svg className="w-4 h-4 ml-1" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" /></svg>,
};

// LOS 3 MODELOS QUE SOLICITASTE
const MODELS = [
    { id: 'gpt-4o', name: 'ChatGPT 4o', cost: '5 créditos / token', desc: 'Simulado vía Gemini (Key 1)' },
    { id: 'claude-3-5', name: 'Claude 3.5 Sonnet', cost: '5 créditos / token', desc: 'Simulado vía Gemini (Key 2)' },
    { id: 'gemini', name: 'Google Gemini', cost: '5 créditos / token', desc: 'Modelo nativo (Key 3)' },
];

export default function Dashboard() {
    const navigate = useNavigate();
    
    // Estados de UI
    const [isSidebarOpen, setIsSidebarOpen] = useState(false);
    const [isProfileDropdownOpen, setIsProfileDropdownOpen] = useState(false);
    const [isModelDropdownOpen, setIsModelDropdownOpen] = useState(false);
    const [activeTab, setActiveTab] = useState('CHAT');
    
    // Estado del Mockup
    const [isOffline, setIsOffline] = useState(false);
    const [selectedModel, setSelectedModel] = useState(MODELS[0]);

    // Estados Lógicos
    const [user, setUser] = useState(null);
    const [isLoading, setIsLoading] = useState(true);
    const [conversations, setConversations] = useState([]);
    const [activeChat, setActiveChat] = useState(null);

    useEffect(() => {
        const fetchInitialData = async () => {
            try {
                const userRes = await authApi.get('/usuarios/me');
                setUser({ email: userRes.data.email, id: userRes.data.id, saldo: userRes.data.saldo || 0 });

                const chatsRes = await chatApi.get('/conversaciones');
                setConversations(chatsRes.data);
                if (chatsRes.data.length > 0) setActiveChat(chatsRes.data[0]);

            } catch (error) { console.error("Error", error); } 
            finally { setIsLoading(false); }
        };
        fetchInitialData();
    }, []);

    const handleNewChat = async () => {
        try {
            const res = await chatApi.post('/conversaciones', { titulo: "Nueva conversación" });
            const newChat = { idConversacion: res.data.idConversacion, tituloChat: "Nueva conversación" };
            setConversations([newChat, ...conversations]);
            setActiveChat(newChat);
            setActiveTab('CHAT');
            setIsSidebarOpen(false);
        } catch (error) { console.error("Error creando chat", error); }
    };

    const handleLogout = () => {
        localStorage.removeItem('token');
        navigate('/login');
    };

    if (isLoading) return <div className="h-screen w-full flex items-center justify-center bg-gray-50">Cargando Hub AI...</div>;

    return (
        <div className="w-full h-screen flex flex-col justify-between bg-gray-50 text-gray-900 relative">
            
            {/* LA BARRA SANDBOX DE TU MOCKUP ORIGINAL */}
            <div className="bg-amber-50 border-b border-amber-200 px-4 py-2 flex flex-wrap gap-4 items-center justify-between z-50 shrink-0 select-none">
                <div className="flex items-center gap-2">
                    <span className="inline-block w-2 h-2 rounded-full bg-amber-500"></span>
                    <span className="font-semibold text-amber-800 text-xs">Entorno de Pruebas Activo</span>
                </div>
                <div className="flex flex-wrap gap-2">
                    <button onClick={() => setIsOffline(!isOffline)} className="px-2 py-1 rounded border border-amber-300 text-amber-700 hover:bg-amber-100 text-[11px] font-medium transition-colors">
                        {isOffline ? 'Restaurar conexión' : 'Simular offline'}
                    </button>
                </div>
            </div>

            {isOffline && (
                <div className="bg-red-500 text-white text-xs py-2 px-4 flex items-center justify-center font-medium shadow-md z-40 shrink-0">
                    Sin conexión a internet. Verifique su red.
                </div>
            )}

            <div className="flex-1 flex flex-col overflow-hidden relative bg-white">
                <header className="h-[60px] min-h-[60px] bg-white border-b border-gray-200 px-4 flex items-center justify-between z-30 select-none shrink-0">
                    <div className="flex items-center gap-4">
                        <button onClick={() => setIsSidebarOpen(!isSidebarOpen)} className="lg:hidden p-1.5 rounded-md hover:bg-gray-100 text-gray-600 transition-colors">
                            <Icons.Menu />
                        </button>
                        <div className="flex items-center gap-2">
                            <div className="bg-blue-600 text-white w-7 h-7 rounded flex items-center justify-center font-bold text-sm">H</div>
                            <span className="font-bold text-gray-900 tracking-tight">Hub AI</span>
                        </div>
                    </div>

                    <div className="flex items-center gap-3">
                        {/* SELECTOR DE MODELOS DE TU MOCKUP */}
                        <div className="relative hidden sm:block">
                            <button onClick={() => setIsModelDropdownOpen(!isModelDropdownOpen)} className="bg-white hover:bg-gray-50 border border-gray-200 px-3 py-1.5 rounded-md text-xs font-medium text-gray-700 flex items-center justify-between min-w-[160px] transition-colors shadow-sm">
                                <div className="flex items-center gap-2 truncate">
                                    <span className="w-1.5 h-1.5 rounded-full bg-green-500"></span>
                                    <span className="truncate">{selectedModel.name}</span>
                                </div>
                                <Icons.ChevronDown />
                            </button>

                            {isModelDropdownOpen && (
                                <div className="absolute top-full left-0 mt-1.5 bg-white border border-gray-200 rounded-md shadow-lg p-1.5 z-50 text-xs min-w-[200px]">
                                    {MODELS.map(m => (
                                        <div key={m.id} onClick={() => { setSelectedModel(m); setIsModelDropdownOpen(false); }}
                                            className={`p-2 rounded-md cursor-pointer transition-colors mb-1 ${selectedModel.id === m.id ? 'bg-blue-50 text-blue-700' : 'hover:bg-gray-50 text-gray-700'}`}>
                                            <div className="font-semibold mb-0.5">{m.name}</div>
                                            <p className="text-[10px] opacity-80 leading-snug">{m.desc}</p>
                                        </div>
                                    ))}
                                </div>
                            )}
                        </div>

                        {/* CRÉDITOS */}
                        <div onClick={() => setActiveTab('PROFILE')} className="hidden sm:flex bg-gray-50 hover:bg-gray-100 border border-gray-200 px-3 py-1.5 rounded-md text-xs items-center gap-2 cursor-pointer transition-colors shadow-sm">
                            <span className="text-gray-500">Créditos:</span>
                            <span className="font-medium text-gray-900">{user?.saldo?.toLocaleString() || 0}</span>
                        </div>

                        {/* PERFIL */}
                        <div className="relative">
                            <button onClick={() => setIsProfileDropdownOpen(!isProfileDropdownOpen)} className="w-8 h-8 rounded-full bg-blue-100 text-blue-700 border border-blue-200 flex items-center justify-center hover:bg-blue-200 transition-colors">
                                <span className="text-xs font-semibold">{user?.email.substring(0, 2).toUpperCase()}</span>
                            </button>

                            {isProfileDropdownOpen && (
                                <div className="absolute right-0 mt-2 w-48 bg-white border border-gray-200 rounded-md shadow-lg p-1 z-50 text-xs">
                                    <div className="px-3 py-2 border-b border-gray-100 mb-1">
                                        <p className="font-semibold text-gray-900 truncate">{user?.email}</p>
                                    </div>
                                    <button onClick={() => { setActiveTab('CHAT'); setIsProfileDropdownOpen(false); }} className={`w-full text-left px-3 py-2 rounded-md ${activeTab === 'CHAT' ? 'bg-blue-50 text-blue-700 font-medium' : 'hover:bg-gray-50 text-gray-700'}`}>
                                        Chat IA
                                    </button>
                                    <button onClick={() => { setActiveTab('PROFILE'); setIsProfileDropdownOpen(false); }} className={`w-full text-left px-3 py-2 rounded-md ${activeTab === 'PROFILE' ? 'bg-blue-50 text-blue-700 font-medium' : 'hover:bg-gray-50 text-gray-700'}`}>
                                        Mi Cuenta / Recargas
                                    </button>
                                    <div className="h-px bg-gray-100 my-1"></div>
                                    <button onClick={handleLogout} className="w-full text-left px-3 py-2 rounded-md hover:bg-red-50 text-red-600 font-medium">
                                        Cerrar Sesión
                                    </button>
                                </div>
                            )}
                        </div>
                    </div>
                </header>

                <div className="flex-1 flex overflow-hidden relative">
                    {/* SIDEBAR DESKTOP */}
                    <aside className="w-[280px] min-w-[280px] bg-gray-50 border-r border-gray-200 hidden lg:flex flex-col p-4 select-none">
                        <button onClick={handleNewChat} className="w-full bg-white hover:bg-gray-50 text-gray-900 font-medium py-2.5 px-4 rounded-lg text-sm flex items-center gap-2 border border-gray-200 transition-colors mb-6 shadow-sm shrink-0">
                            <Icons.Plus />
                            <span>Nuevo chat</span>
                        </button>
                        <div className="text-xs text-gray-500 font-semibold mb-3 px-1">Recientes</div>
                        <div className="flex-1 overflow-y-auto space-y-1">
                            {conversations.map(c => (
                                <div key={c.idConversacion} onClick={() => { setActiveChat(c); setActiveTab('CHAT'); }}
                                    className={`rounded-lg cursor-pointer p-3 transition-colors ${activeChat?.idConversacion === c.idConversacion ? 'bg-blue-50 text-blue-900 font-medium' : 'hover:bg-gray-100 text-gray-700 text-sm'}`}>
                                    {c.tituloChat}
                                </div>
                            ))}
                        </div>
                    </aside>

                    <main className="flex-1 flex flex-col bg-white overflow-hidden">
                        {activeTab === 'CHAT' && (
                            <ChatArea 
                                activeChat={activeChat} 
                                onCreditsUpdated={(nuevoSaldo) => setUser({...user, saldo: nuevoSaldo})}
                                selectedModel={selectedModel} /* <--- LE PASAMOS EL MODELO A CHATAREA */
                            />
                        )}
                        {activeTab === 'PROFILE' && (
                            <BillingArea user={user} onCreditsUpdated={(nuevoSaldo) => setUser({...user, saldo: nuevoSaldo})} />
                        )}
                    </main>
                </div>
            </div>
        </div>
    );
}