import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import App from './App';
import './index.css';
import { EntityTooltipProvider } from './components/layout/EntityTooltipProvider';
import { AppStateProvider } from './state/AppStateContext';

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
