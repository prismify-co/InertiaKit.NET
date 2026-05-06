import { createInertiaApp } from '@inertiajs/react';
import { createRoot } from 'react-dom/client';
import { createInertiaPageResolver } from '../../../shared/resolveInertiaPage.js';
import '@picocss/pico/css/pico.min.css';
import './styles.css';

const pages = import.meta.glob('./Pages/**/*.jsx', { eager: true });
const initialPage = JSON.parse(document.getElementById('app-data')?.textContent ?? '{}');
const resolvePage = createInertiaPageResolver({
  pages,
  extension: '.jsx',
  frameworkName: 'React',
});

createInertiaApp({
  page: initialPage,
  title: (title) => (title ? `${title} | InertiaKit React` : 'InertiaKit React'),
  resolve: resolvePage,
  setup({ el, App, props }) {
    createRoot(el).render(<App {...props} />);
  },
});