export default defineNuxtPlugin((nuxtApp) => {
  const modalStack = []
  const BODY_LOCK_CLASS = 'overflow-hidden h-full'

  function lockBodyScroll() {
    document.body.classList.add(...BODY_LOCK_CLASS.split(' '))
  }

  function unlockBodyScroll() {
    document.body.classList.remove(...BODY_LOCK_CLASS.split(' '))
  }

  function openModal(id) {
    const modal = document.getElementById(id)
    if (!modal) return console.warn('Modal not found:', id)
    if (modalStack.includes(modal)) return

    modal.classList.remove('hidden')

    const backdrop = modal.querySelector('.modal-backdrop')
    const panel = modal.querySelector('.modal-panel')
    const base = 1000 + modalStack.length * 20

    if (backdrop) backdrop.style.zIndex = base
    if (panel) {
      panel.style.zIndex = base + 10
      panel.classList.add('scale-95', 'opacity-0', 'transition-all', 'duration-200', 'ease-out')

      requestAnimationFrame(() => {
        panel.classList.remove('scale-95', 'opacity-0')
        panel.classList.add('scale-100', 'opacity-100')
      })
    }

    modalStack.push(modal)
    lockBodyScroll()

    // --- EVENT shown ---
    setTimeout(() => {
      modal.dispatchEvent(new CustomEvent('shown'))
    }, 100)
  }

  function closeModal(id) {
    const modal = document.getElementById(id)
    if (!modal) return
    const idx = modalStack.indexOf(modal)
    if (idx === -1) return

    const panel = modal.querySelector('.modal-panel')
    if (panel) {
      panel.classList.remove('scale-100', 'opacity-100')
      panel.classList.add('scale-95', 'opacity-0')

      setTimeout(() => {
        modal.classList.add('hidden')
        panel.classList.remove('scale-95', 'opacity-0')

        // --- EVENT hidden ---
        modal.dispatchEvent(new CustomEvent('hidden'))
      }, 100)

    } else {
      modal.classList.add('hidden')
      modal.dispatchEvent(new CustomEvent('hidden'))
    }

    modalStack.splice(idx, 1)
    if (modalStack.length === 0) unlockBodyScroll()
  }

  nuxtApp.provide('modal', {
    show: openModal,
    hide: closeModal
  })
})
