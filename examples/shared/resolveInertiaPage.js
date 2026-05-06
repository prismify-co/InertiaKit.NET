const SingleSegmentPattern = /^\[(?!\.\.\.)([^\]]+)\]$/;
const CatchAllPattern = /^\[\.\.\.([^\]]+)\]$/;
const OptionalCatchAllPattern = /^\[\[\.\.\.([^\]]+)\]\]$/;

function parseSegment(segment) {
  if (OptionalCatchAllPattern.test(segment)) {
    return { type: 'optional-catch-all' };
  }

  if (CatchAllPattern.test(segment)) {
    return { type: 'catch-all' };
  }

  if (SingleSegmentPattern.test(segment)) {
    return { type: 'dynamic' };
  }

  return { type: 'static', value: segment };
}

function matchSegments(patternSegments, valueSegments, patternIndex = 0, valueIndex = 0) {
  if (patternIndex === patternSegments.length) {
    return valueIndex === valueSegments.length;
  }

  const segment = parseSegment(patternSegments[patternIndex]);

  if (segment.type === 'static') {
    return valueIndex < valueSegments.length
      && segment.value === valueSegments[valueIndex]
      && matchSegments(patternSegments, valueSegments, patternIndex + 1, valueIndex + 1);
  }

  if (segment.type === 'dynamic') {
    return valueIndex < valueSegments.length
      && matchSegments(patternSegments, valueSegments, patternIndex + 1, valueIndex + 1);
  }

  const minimumConsumed = segment.type === 'catch-all' ? 1 : 0;

  for (let nextValueIndex = valueIndex + minimumConsumed; nextValueIndex <= valueSegments.length; nextValueIndex += 1) {
    if (matchSegments(patternSegments, valueSegments, patternIndex + 1, nextValueIndex)) {
      return true;
    }
  }

  return false;
}

function calculateSpecificity(patternSegments) {
  return patternSegments.reduce((score, segment) => {
    const token = parseSegment(segment);

    switch (token.type) {
      case 'static':
        return score + 100;
      case 'dynamic':
        return score + 10;
      case 'catch-all':
        return score + 1;
      case 'optional-catch-all':
        return score;
      default:
        return score;
    }
  }, 0);
}

function toPatternSegments(pagePath, extension) {
  return pagePath
    .slice('./Pages/'.length, -extension.length)
    .split('/')
    .filter((segment) => segment.length > 0);
}

function toComponentSegments(componentName) {
  return componentName.split('/').filter((segment) => segment.length > 0);
}

export function createInertiaPageResolver({ pages, extension, frameworkName }) {
  const knownPages = Object.entries(pages).map(([path, pageModule]) => ({
    path,
    pageModule,
    patternSegments: toPatternSegments(path, extension),
  }));

  return (componentName) => {
    const exactPath = `./Pages/${componentName}${extension}`;
    const exactPage = pages[exactPath];

    if (exactPage) {
      return exactPage.default ?? exactPage;
    }

    const componentSegments = toComponentSegments(componentName);
    const candidates = knownPages
      .filter((page) => page.path.includes('['))
      .filter((page) => matchSegments(page.patternSegments, componentSegments))
      .map((page) => ({
        ...page,
        specificity: calculateSpecificity(page.patternSegments),
      }))
      .sort((left, right) => {
        if (right.specificity !== left.specificity) {
          return right.specificity - left.specificity;
        }

        if (right.patternSegments.length !== left.patternSegments.length) {
          return right.patternSegments.length - left.patternSegments.length;
        }

        return left.path.localeCompare(right.path);
      });

    if (candidates.length === 0) {
      throw new Error(`Missing ${frameworkName} page component: ${componentName}`);
    }

    if (candidates.length > 1 && candidates[0].specificity === candidates[1].specificity) {
      const matchedPaths = candidates
        .filter((candidate) => candidate.specificity === candidates[0].specificity)
        .map((candidate) => candidate.path)
        .join(', ');

      throw new Error(`Ambiguous ${frameworkName} page component: ${componentName}. Matched: ${matchedPaths}`);
    }

    return candidates[0].pageModule.default ?? candidates[0].pageModule;
  };
}