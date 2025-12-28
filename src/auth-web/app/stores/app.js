
export const useApp = defineStore('App', {
  state: () => ({
    list: [],
  }),
  actions: {
    setMenu(module, menu) {
      this.list = [];
      this.list.push(module)
      this.list.push(menu)
    },
    setMenus(list) {
      this.list = list;
    }
  },
});

if (import.meta.hot) {
  import.meta.hot.accept(acceptHMRUpdate(useApp, import.meta.hot));
}