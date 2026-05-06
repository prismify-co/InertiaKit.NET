import { defineConfig } from 'vite';

export default defineConfig({
  server: {
    host: '127.0.0.1',
    port: 5175,
    strictPort: true,
    cors: true,
  },
  build: {
    outDir: '../wwwroot/fastendpoints',
    emptyOutDir: true,
    cssCodeSplit: false,
    rollupOptions: {
      input: 'src/app.js',
      output: {
        entryFileNames: 'app.js',
        assetFileNames: ({ name }) =>
          name?.endsWith('.css') ? 'app.css' : 'assets/[name]-[hash][extname]',
      },
    },
  },
});