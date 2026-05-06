import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';

export default defineConfig({
  plugins: [vue()],
  server: {
    host: '127.0.0.1',
    port: 5174,
    strictPort: true,
    cors: true,
  },
  build: {
    outDir: '../wwwroot/build',
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