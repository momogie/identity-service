import { reactive } from 'vue'

export default defineNuxtPlugin((nuxtApp) => {
  const state = reactive({
    show: false,
    title: '',
    message: '',
    type: 'success'
  })

  function open(type: string, title: string, message: string) {
    state.type = type
    state.title = title
    state.message = message
    state.show = true
  }

  const toast = {
    success: (title: string, message: string) => open('success', title, message),
    error:   (title: string, message: string) => open('error', title, message),
    info:    (title: string, message: string) => open('info', title, message),
  }

  nuxtApp.provide('alert', toast)
  nuxtApp.provide('alertState', state)
  // return {
  //   provide: {
  //     toast,
  //     toastState: state
  //   }
  // }
})
