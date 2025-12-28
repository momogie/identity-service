var app = useNuxtApp();

export const useTreeSource = defineStore('TreeSource', {
  state: () => ({
    isLoading: false,
    isServerError: false,
    isNetworkError: false,
    requestCounter: 0,
    controller: new AbortController(),
    data: {},
    filter: {
    },
  }),
  actions: {
    setActionController: function(contoller, action) {
    },
    load: function() {
      if(!this.controller?.signal?.aborted) {
        this.controller?.abort();
      }
      this.controller = new AbortController();
      this.isLoading = true;
      this.isNetworkError = this.isServerError = false;
      this.data = {};
      this.requestCounter++;
      return new Promise((resolve, reject) => {
        
      })
    },
    abortRequest: function() {
      if(!this.isLoading) return;
      this.controller.abort();
    },
  },
});

if (import.meta.hot) {
  import.meta.hot.accept(acceptHMRUpdate(useTreeSource, import.meta.hot));
}