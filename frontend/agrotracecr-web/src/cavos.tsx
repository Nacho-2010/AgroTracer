import { createContext, useContext, type ReactNode } from 'react';
import { CavosProvider, useCavos } from '@cavos/kit/react';

const env = (import.meta as unknown as { env: Record<string, string> }).env;
const appId = env.VITE_CAVOS_APP_ID?.trim();
type EmbeddedWallet = {
  configured: boolean;
  address: string | null;
  loading: boolean;
  error: string | null;
  needsApproval: boolean;
  login: () => void;
  logout: () => void;
  sign: (xdr: string) => Promise<string>;
};
const unavailable = () => {
  throw new Error('Configura VITE_CAVOS_APP_ID para habilitar Cavos.');
};
const WalletContext = createContext<EmbeddedWallet>({
  configured: false,
  address: null,
  loading: false,
  error: null,
  needsApproval: false,
  login: unavailable,
  logout: () => {},
  sign: async () => unavailable(),
});
function ConnectedWallet({ children }: { children: ReactNode }) {
  const cavos = useCavos();
  return (
    <WalletContext.Provider
      value={{
        configured: true,
        address: cavos.wallet?.chain === 'stellar' ? cavos.address : null,
        loading: cavos.isLoading,
        error: cavos.authError,
        needsApproval: cavos.walletStatus.needsDeviceApproval,
        login: cavos.openModal,
        logout: cavos.logout,
        sign: async (xdr) => {
          if (cavos.wallet?.chain !== 'stellar')
            throw new Error('Inicia sesión con Cavos para continuar.');
          if (cavos.wallet.status === 'needs-device-approval')
            throw new Error('Autoriza este dispositivo antes de pagar.');
          return cavos.wallet.signXdr(xdr);
        },
      }}
    >
      {children}
    </WalletContext.Provider>
  );
}
export function AgroWalletProvider({ children }: { children: ReactNode }) {
  if (!appId) return children;
  return (
    <CavosProvider
      config={{
        appId,
        chains: ['stellar'],
        defaultChain: 'stellar',
        network: 'testnet',
        environment:
          env.VITE_CAVOS_ENVIRONMENT === 'production'
            ? 'production'
            : 'development',
        appSalt: env.VITE_CAVOS_APP_SALT?.trim() || 'agrotracecr-stellar-v1',
        socialRecovery: true,
        deviceApproval: 'enclave',
      }}
      modal={{
        appName: 'AgroTraceCR',
        theme: 'light',
        providers: ['google', 'email'],
        primaryColor: '#315b3d',
      }}
    >
      <ConnectedWallet>{children}</ConnectedWallet>
    </CavosProvider>
  );
}
export const useEmbeddedWallet = () => useContext(WalletContext);
