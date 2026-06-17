import { useState, useEffect } from 'react';
import { billingApi } from '../services/api';

const Icons = {
    Close: () => <svg className="w-5 h-5 text-gray-500" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" /></svg>,
};

export default function BillingArea({ user, onCreditsUpdated }) {
    const [transactions, setTransactions] = useState([]);
    const [isLoading, setIsLoading] = useState(true);

    // Estados del Modal de Compra (Restaurados a tu diseño original)
    const [isPurchaseModalOpen, setIsPurchaseModalOpen] = useState(false);
    const [wizardStep, setWizardStep] = useState(1);
    const [purchaseSelectedId, setPurchaseSelectedId] = useState('p2'); 
    const [cardName, setCardName] = useState('');
    const [cardNumber, setCardNumber] = useState('');
    const [cardExpiry, setCardExpiry] = useState('');
    const [cardCvv, setCardCvv] = useState('');
    const [processingMessage, setProcessingMessage] = useState('Procesando pago...');

    const purchasePackages = [
        { id: 'p1', price: 10, credits: 10000, desc: 'Básico' },
        { id: 'p2', price: 25, credits: 30000, desc: 'Profesional' },
        { id: 'p3', price: 50, credits: 70000, desc: 'Avanzado' },
        { id: 'p4', price: 100, credits: 150000, desc: 'Experto' }
    ];

    const fetchTransactions = async () => {
        try {
            const res = await billingApi.get('/pagos/transacciones');
            setTransactions(res.data.items || []);
        } catch (error) {
            console.error("Error cargando transacciones", error);
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        fetchTransactions();
    }, []);

    const proceedToPayment = () => setWizardStep(2);

    const processCardPayment = async (e) => {
        e.preventDefault();
        setWizardStep(3);
        setProcessingMessage('Conectando con el banco...');

        const selectedPkg = purchasePackages.find(p => p.id === purchaseSelectedId);

        try {
            const res = await billingApi.post('/pagos/cargar-saldo', {
                montoDinero: selectedPkg.price,
                moneda: "USD"
            });

            setTimeout(() => {
                setProcessingMessage('Autorizando transacción...');
                setTimeout(() => {
                    onCreditsUpdated(res.data.nuevoSaldo);
                    fetchTransactions();
                    setWizardStep(4);
                }, 1500);
            }, 1500);

        } catch (error) {
            console.error("Error al procesar el pago", error);
            alert("Hubo un error procesando el pago. Verifica que el servidor de facturación esté activo.");
            setIsPurchaseModalOpen(false);
        }
    };

    const openPurchaseModal = () => {
        setWizardStep(1);
        setCardName(''); setCardNumber(''); setCardExpiry(''); setCardCvv('');
        setIsPurchaseModalOpen(true);
    };

    return (
        <div className="flex-1 overflow-y-auto p-4 sm:p-8 space-y-8 max-w-5xl mx-auto w-full text-left">
            <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between border-b border-gray-200 pb-4 gap-4">
                <div>
                    <h2 className="text-2xl font-bold text-gray-900">Mi Cuenta</h2>
                    <p className="text-gray-500 text-sm mt-1">Gestiona tu saldo y revisa tu historial de transacciones.</p>
                </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div className="bg-white border border-gray-200 rounded-xl p-6 shadow-sm flex flex-col justify-between">
                    <div className="text-sm font-medium text-gray-500 mb-4">Saldo Disponible</div>
                    <div className="flex items-baseline gap-1 mb-2">
                        <span className="text-4xl font-bold text-gray-900">{user?.saldo?.toLocaleString() || 0}</span>
                        <span className="text-sm text-gray-500">créditos</span>
                    </div>
                    <button onClick={openPurchaseModal} className="mt-4 w-full bg-white hover:bg-gray-50 border border-gray-300 text-gray-700 text-sm font-medium py-2 rounded-lg transition-colors">
                        Recargar Saldo
                    </button>
                </div>

                <div className="bg-white border border-gray-200 rounded-xl p-6 shadow-sm flex flex-col justify-between">
                    <div className="text-sm font-medium text-gray-500 mb-4">Detalles del Perfil</div>
                    <div className="space-y-4">
                        <div className="flex justify-between text-sm mb-1">
                            <span className="text-gray-700">Usuario</span>
                            <span className="font-medium text-gray-900">{user?.email}</span>
                        </div>
                        <div className="flex justify-between text-sm mb-1">
                            <span className="text-gray-700">Estado</span>
                            <span className="font-medium text-green-600">Activo</span>
                        </div>
                    </div>
                </div>
            </div>

            <div className="bg-white border border-gray-200 rounded-xl overflow-hidden shadow-sm">
                <div className="p-4 border-b border-gray-200 bg-gray-50">
                    <h3 className="text-sm font-bold text-gray-900">Historial de Transacciones</h3>
                </div>
                <div className="overflow-x-auto">
                    <table className="w-full text-sm text-left">
                        <thead>
                            <tr className="border-b border-gray-200 bg-white text-gray-500 font-medium">
                                <th className="p-4">Fecha</th>
                                <th className="p-4">Tipo</th>
                                <th className="p-4">Monto</th>
                                <th className="p-4">Referencia</th>
                            </tr>
                        </thead>
                        <tbody>
                            {isLoading ? (
                                <tr><td colSpan="4" className="p-4 text-center text-gray-500">Cargando...</td></tr>
                            ) : transactions.length === 0 ? (
                                <tr><td colSpan="4" className="p-4 text-center text-gray-500">No hay transacciones.</td></tr>
                            ) : (
                                transactions.map((tx) => {
                                    const isPositive = tx.tipoTransaccion === 'CARGA_PAYMENT';
                                    return (
                                        <tr key={tx.idTransaccion} className="border-b border-gray-100 hover:bg-gray-50">
                                            <td className="p-4 text-gray-600">{new Date(tx.fechaHora).toLocaleString()}</td>
                                            <td className="p-4">
                                                <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${isPositive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'}`}>
                                                    {tx.tipoTransaccion.replace('_', ' ')}
                                                </span>
                                            </td>
                                            <td className={`p-4 font-medium ${isPositive ? 'text-green-600' : 'text-gray-900'}`}>
                                                {isPositive ? '+' : '-'} {tx.montoCreditos.toLocaleString()} cr
                                            </td>
                                            <td className="p-4 text-gray-500 font-mono text-xs">{tx.referenciaId.split('-')[0].toUpperCase()}</td>
                                        </tr>
                                    );
                                })
                            )}
                        </tbody>
                    </table>
                </div>
            </div>

            {/* Modal de Recarga Exacto a tu Mockup */}
            {isPurchaseModalOpen && (
                <div className="fixed inset-0 bg-gray-900/40 flex items-center justify-center p-4 z-50">
                    <div className="bg-white border border-gray-200 rounded-xl w-full max-w-[500px] shadow-xl overflow-hidden flex flex-col">
                        
                        <div className="p-5 border-b border-gray-100 flex items-center justify-between bg-white shrink-0">
                            <h3 className="text-lg font-bold text-gray-900">Recargar Créditos</h3>
                            <button onClick={() => setIsPurchaseModalOpen(false)} className="text-gray-400 hover:text-gray-600"><Icons.Close /></button>
                        </div>

                        <div className="p-6 overflow-y-auto">
                            {wizardStep === 1 && (
                                <div className="space-y-4">
                                    <p className="text-sm text-gray-600">Selecciona el paquete que mejor se adapte a tu uso:</p>
                                    <div className="grid grid-cols-2 gap-4">
                                        {purchasePackages.map(pkg => (
                                            <div key={pkg.id} onClick={() => setPurchaseSelectedId(pkg.id)}
                                                className={`p-4 rounded-xl border-2 cursor-pointer transition-all ${purchaseSelectedId === pkg.id ? 'border-blue-600 bg-blue-50' : 'border-gray-200 bg-white hover:border-gray-300'}`}>
                                                <div className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-1">{pkg.desc}</div>
                                                <div className="text-xl font-bold text-gray-900">${pkg.price} <span className="text-sm font-normal text-gray-500">USD</span></div>
                                                <div className="mt-2 text-sm font-medium text-blue-600">{pkg.credits.toLocaleString()} cr</div>
                                            </div>
                                        ))}
                                    </div>
                                    <div className="flex justify-end pt-2">
                                        <button onClick={proceedToPayment} className="w-full bg-blue-600 hover:bg-blue-700 text-white font-medium text-sm py-2.5 rounded-lg transition-colors">
                                            Continuar
                                        </button>
                                    </div>
                                </div>
                            )}

                            {wizardStep === 2 && (
                                <form onSubmit={processCardPayment} className="space-y-4">
                                    <div className="space-y-1">
                                        <label className="block text-sm font-medium text-gray-700">Nombre en la tarjeta</label>
                                        <input type="text" required placeholder="Ej. Juan Díaz" value={cardName} onChange={(e) => setCardName(e.target.value.toUpperCase())}
                                            className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm text-gray-900 focus:outline-none focus:ring-2 focus:ring-blue-500" />
                                    </div>
                                    <div className="space-y-1">
                                        <label className="block text-sm font-medium text-gray-700">Número de Tarjeta</label>
                                        <input type="text" required placeholder="0000 0000 0000 0000" value={cardNumber} onChange={(e) => setCardNumber(e.target.value)}
                                            className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm text-gray-900 focus:outline-none focus:ring-2 focus:ring-blue-500" />
                                    </div>
                                    <div className="grid grid-cols-2 gap-4">
                                        <div className="space-y-1">
                                            <label className="block text-sm font-medium text-gray-700">Vencimiento</label>
                                            <input type="text" required placeholder="MM/AA" value={cardExpiry} onChange={(e) => setCardExpiry(e.target.value)}
                                                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm text-center focus:outline-none focus:ring-2 focus:ring-blue-500" />
                                        </div>
                                        <div className="space-y-1">
                                            <label className="block text-sm font-medium text-gray-700">CVV</label>
                                            <input type="password" required placeholder="123" value={cardCvv} onChange={(e) => setCardCvv(e.target.value)}
                                                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm text-center focus:outline-none focus:ring-2 focus:ring-blue-500" />
                                        </div>
                                    </div>
                                    <div className="flex gap-3 mt-4">
                                        <button type="button" onClick={() => setWizardStep(1)} className="px-4 py-2 bg-white border border-gray-300 text-gray-700 text-sm font-medium rounded-lg hover:bg-gray-50">Atrás</button>
                                        <button type="submit" className="flex-1 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-lg py-2">
                                            Pagar ${purchasePackages.find(p => p.id === purchaseSelectedId)?.price}
                                        </button>
                                    </div>
                                </form>
                            )}

                            {wizardStep === 3 && (
                                <div className="py-12 flex flex-col items-center justify-center text-center space-y-4">
                                    <div className="w-10 h-10 border-4 border-blue-200 border-t-blue-600 rounded-full animate-spin"></div>
                                    <p className="text-sm font-medium text-gray-900">{processingMessage}</p>
                                </div>
                            )}

                            {wizardStep === 4 && (
                                <div className="py-8 flex flex-col items-center justify-center text-center space-y-3">
                                    <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center text-green-600 text-3xl mb-2">✓</div>
                                    <h4 className="text-lg font-bold text-gray-900">¡Pago Exitoso!</h4>
                                    <p className="text-gray-500 text-sm">Tus créditos han sido añadidos a tu cuenta.</p>
                                    <button onClick={() => setIsPurchaseModalOpen(false)} className="w-full mt-4 bg-blue-600 hover:bg-blue-700 text-white font-medium text-sm py-2.5 rounded-lg">Entendido</button>
                                </div>
                            )}
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}