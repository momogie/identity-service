
export const useTheme = defineStore('Theme', {
  state: () => ({
    selectedTheme: { key: 'default', name: 'Default'},
    list: [
      { key: 'default', name: 'Default'},
      { key: 'amethyst-haze', name: 'Amethyst Haze'},
      { key: 'pastel-dreams', name: 'Pastel Dreams'},
      { key: 'twitter', name: 'Twitter'},
      { key: 'mocha-mousse', name: 'Mocha Mousse'},
      { key: 'retro-arcade', name: 'Retro Arcade'},
      { key: 'solarized-dusk', name: 'Solarized Dusk'},
      { key: 'bubblegum', name: 'Bubblegum'},
      { key: 'neo-brutalism', name: 'Neo Brutalism'},
    ],
  }),
  actions: {
    setTheme(key) {
      document.documentElement.setAttribute('data-theme', key);
      localStorage.setItem('theme', key);
      this.selectedTheme = this.list.find(p => p.key == key)
    },
    load: function() {
      var key = localStorage.getItem('theme')
      if(!key) {
        document.documentElement.setAttribute('data-theme', 'default');
        localStorage.setItem('theme',  'default');

        this.selectedTheme = this.list.find(p => p.key == key)
        return;
      }
      document.documentElement.setAttribute('data-theme', key);
      this.selectedTheme = this.list.find(p => p.key == key);
    },
    toggleDarkMode: function(e) {
      // document.documentElement.clas('data-theme', key);
    },
  },
});

if (import.meta.hot) {
  import.meta.hot.accept(acceptHMRUpdate(useTheme, import.meta.hot));
}