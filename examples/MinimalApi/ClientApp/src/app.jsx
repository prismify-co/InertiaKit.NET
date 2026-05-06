import { createInertiaApp } from '@inertiajs/react';
import { createRoot } from 'react-dom/client';
import { createInertiaPageResolver } from '../../../shared/resolveInertiaPage.js';
import '@fontsource/geist-sans/400.css';
import '@fontsource/geist-sans/500.css';
import '@fontsource/geist-sans/600.css';
import '@fontsource/geist-sans/700.css';
import '@fontsource/geist-mono/400.css';
import '../../../shared/design-system.css';
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
  title: (title) => (title ? `${title} | Northstar Launch Ops` : 'Northstar Launch Ops'),
  resolve: resolvePage,
  setup({ el, App, props }) {
    createRoot(el).render(<App {...props} />);
  },
});