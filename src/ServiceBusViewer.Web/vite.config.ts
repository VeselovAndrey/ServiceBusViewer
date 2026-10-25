import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import { defineConfig, loadEnv } from 'vite';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const proxyTarget = env.VITE_PROXY_TARGET ?? 'http://localhost:5221';
  const port = Number.parseInt(env.PORT ?? '5173', 10);

  return {
    plugins: [react(), tailwindcss()],
    server: {
      // Keep the development server and its API proxy reachable only from this machine.
      host: 'localhost',
      port,
      strictPort: true,
      proxy: {
        '/api': {
          changeOrigin: true,
          secure: false,
          target: proxyTarget,
        },
      },
    },
  };
});
