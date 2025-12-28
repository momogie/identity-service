import tailwindcss from '@tailwindcss/vite'
// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  devtools: { enabled: true },
  ssr: false, // non-SSR
  runtimeConfig: {
    // apiSecret: 'server-only-value',        // hanya server
    public: {
      // baseUrl: 'http://localhost:65293/api'    //
      baseUrl: 'http://localhost:5190/api'    //
    }
  },
  modules: [
    '@nuxt/icon', 'shadcn-nuxt', '@pinia/nuxt'
  ],
  css: ['~/assets/css/tailwind.css', '~/assets/app.scss'],
  vite: {
    plugins: [
      tailwindcss(),
    ],
  },
  shadcn: {
    /**
     * Prefix for all the imported component
     */
    prefix: '',
    /**
     * Directory that the component lives in.
     * @default "./components/ui"
     */
    componentDir: './app/components/shadcn/components/ui',
  }
})