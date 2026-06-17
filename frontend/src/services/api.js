import axios from 'axios';

const createApiClient = (baseURL) => {
  const api = axios.create({
    baseURL,
  });

  api.interceptors.request.use(
    (config) => {
      const token = localStorage.getItem('token');
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      return config;
    },
    (error) => Promise.reject(error)
  );

  api.interceptors.response.use(
    (response) => response,
    (error) => {
      if (error.response && error.response.status === 401) {
        localStorage.removeItem('token');
        window.location.href = '/login';
      }
      return Promise.reject(error);
    }
  );

  return api;
};

export const authApi = createApiClient(import.meta.env.VITE_AUTH_API || 'http://localhost:5018');
export const billingApi = createApiClient(import.meta.env.VITE_BILLING_API || 'http://localhost:5020');
export const chatApi = createApiClient(import.meta.env.VITE_CHAT_API || 'http://localhost:5000');
