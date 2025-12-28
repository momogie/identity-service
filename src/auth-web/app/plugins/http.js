import axios from 'axios';

export default defineNuxtPlugin(nuxtApp => {

  const config = useRuntimeConfig()
    
  const BASE_URL = process.dev ? config.public.baseUrl : '/api';
  const http = axios.create({
    baseURL: BASE_URL,
    withCredentials: true,
  });

  http.interceptors.response.use(
    (response) => response,
    (error) => {

      if(error.status == 401)
        location.href = `${BASE_URL}/../auth/signin?returnUrl=` + location.href;

      return Promise.reject(error);
    }
  )
  nuxtApp.provide('http', {
    ...http,
    open: function(path, target = '_blank') {
      // if(path.includes("?")) {
      //   path = path + '&workspace=' + route.params?.id;
      // }
      // else {
      //   path = path + '?workspace=' + route.params?.id;
      // }
      window.open(BASE_URL + path, target)
    }
  })
})