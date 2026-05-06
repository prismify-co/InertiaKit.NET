import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';

export default defineConfig({
  plugins: [vue()],
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