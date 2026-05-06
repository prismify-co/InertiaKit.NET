import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  build: {
    outDir: '../wwwroot/build',
    emptyOutDir: true,
    cssCodeSplit: false,
    rollupOptions: {
      input: 'src/app.jsx',
      output: {
        entryFileNames: 'app.js',
        assetFileNames: ({ name }) =>
          name?.endsWith('.css') ? 'app.css' : 'assets/[name]-[hash][extname]',
      },
    },
  },
});