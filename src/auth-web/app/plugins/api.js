// import { BASE_URL } from '@/config/constants';
import axios from "axios";

export default defineNuxtPlugin((nuxtApp) => {

  const config = useRuntimeConfig();

  const BASE_URL = process.dev ? config.public.baseUrl : "/api";
  const http = axios.create({
    baseURL: BASE_URL,
    withCredentials: true,
  });

  http.interceptors.response.use(
    (response) => response,
    (error) => {
      if (error.status == 401) location.href = `${BASE_URL}/../auth/signin?returnUrl=` + location.href;

      return Promise.reject(error);
    }
  );
  nuxtApp.provide("api", {
    ...http,
    open: function (path, target = "_blank") {
      window.open(BASE_URL + path, target);
    },
  });
});
