import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import App from './App';
import './index.css';
import { EntityTooltipProvider } from './components/layout/EntityTooltipProvider';
import { AppStateProvider } from './state/AppStateContext';

async function installTooltipPolyfills() {
  // Conditional, bundled polyfills for browsers without native support.
  if (!('popover' in HTMLElement.prototype)) {
    await import('@oddbird/popover-polyfill');
  }
  if (!('anchorName' in document.documentElement.style)) {
    await import('@oddbird/css-anchor-positioning');
  }
}

void installTooltipPolyfills().then(() => {
  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <BrowserRouter>
        <AppStateProvider>
          <EntityTooltipProvider>
            <App />
          </EntityTooltipProvider>
        </AppStateProvider>
      </BrowserRouter>
    </StrictMode>,
  );
});
