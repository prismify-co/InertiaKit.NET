import { createApp, h } from 'vue';
import { createInertiaApp } from '@inertiajs/vue3';
import { createInertiaPageResolver } from '../../../shared/resolveInertiaPage.js';
import './styles.css';

const pages = import.meta.glob('./Pages/**/*.vue', { eager: true });
const initialPage = JSON.parse(document.getElementById('app-data')?.textContent ?? '{}');
const resolvePage = createInertiaPageResolver({
  pages,
  extension: '.vue',
  frameworkName: 'Vue',
});

createInertiaApp({
  page: initialPage,
  title: (title) => (title ? `${title} | InertiaKit Vue` : 'InertiaKit Vue'),
  resolve: resolvePage,
  setup({ el, App, props, plugin }) {
    createApp({ render: () => h(App, props) }).use(plugin).mount(el);
  },
});