import test from 'node:test';
import assert from 'node:assert/strict';
import { createInertiaPageResolver } from '../../examples/shared/resolveInertiaPage.js';

function createPageModule(name) {
  return { default: { name } };
}

test('resolves exact page components before dynamic fallbacks', () => {
  const resolvePage = createInertiaPageResolver({
    pages: {
      './Pages/Docs/Overview.jsx': createPageModule('exact'),
      './Pages/Docs/[slug].jsx': createPageModule('dynamic'),
    },
    extension: '.jsx',
    frameworkName: 'React',
  });

  assert.equal(resolvePage('Docs/Overview').name, 'exact');
});

test('resolves single-segment dynamic page files', () => {
  const resolvePage = createInertiaPageResolver({
    pages: {
      './Pages/Blog/[slug].jsx': createPageModule('dynamic'),
    },
    extension: '.jsx',
    frameworkName: 'React',
  });

  assert.equal(resolvePage('Blog/hello-world').name, 'dynamic');
});

test('resolves catch-all page files', () => {
  const resolvePage = createInertiaPageResolver({
    pages: {
      './Pages/Docs/[...page].jsx': createPageModule('catch-all'),
    },
    extension: '.jsx',
    frameworkName: 'React',
  });

  assert.equal(resolvePage('Docs/guides/getting-started').name, 'catch-all');
});

test('resolves optional catch-all page files for empty tails', () => {
  const resolvePage = createInertiaPageResolver({
    pages: {
      './Pages/Docs/[[...page]].jsx': createPageModule('optional-catch-all'),
    },
    extension: '.jsx',
    frameworkName: 'React',
  });

  assert.equal(resolvePage('Docs').name, 'optional-catch-all');
});

test('prefers more specific dynamic matches over broader catch-all matches', () => {
  const resolvePage = createInertiaPageResolver({
    pages: {
      './Pages/Docs/[...page].jsx': createPageModule('catch-all'),
      './Pages/Docs/[section]/[slug].jsx': createPageModule('nested-dynamic'),
    },
    extension: '.jsx',
    frameworkName: 'React',
  });

  assert.equal(resolvePage('Docs/guides/getting-started').name, 'nested-dynamic');
});

test('throws when two equally specific dynamic pages would match the same component', () => {
  const resolvePage = createInertiaPageResolver({
    pages: {
      './Pages/Docs/[slug].jsx': createPageModule('slug'),
      './Pages/Docs/[id].jsx': createPageModule('id'),
    },
    extension: '.jsx',
    frameworkName: 'React',
  });

  assert.throws(
    () => resolvePage('Docs/example'),
    /Ambiguous React page component: Docs\/example/);
});