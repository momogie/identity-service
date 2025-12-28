
var app = useNuxtApp();
import axios from "axios";
export const useGridUrl = (key) => defineStore(key, {
  state: () => ({
    isLoading: false,
    isServerError: false,
    isNetworkError: false,
    controller: new AbortController(),
    url: null,
    data: {
      Headers: [],
      Items: [],
      Total: 0,
      Filtered: 0,
      Page: 1,
      Length: 25,
    },
    filter: {
      Page: 1,
      Length: 25,
      Filters: [],
      Sorts: {},
    },
    columnList: [],
  }),
  actions: {
    setUrl: function(v) {
      this.url = '/..' + v;
    },
    load: function() {
      this.isLoading = true;
      this.isNetworkError = this.isServerError = false;
      this.data.Items = [];
      return new Promise((resolve, reject) => {
        app.$http.post(this.url, this.filter)
          .then(({data}) => {
            this.data = data.Data;
            // var pref = usePreference();
            // const config = pref.data.find(p => p.Key == `GRID_${this.filter.DataSource.toUpperCase()}`);
            // if(config) {
            //   this.data.Headers = this.columnList = JSON.parse(config.Value).Header
            // }
            resolve(data);
          })
          .catch(err => {
            if(err.code == 'ERR_NETWORK')
              this.isNetworkError = true;
            
            if(err.code == 'ERR_BAD_RESPONSE')
              this.isServerError = true;

            reject(err);
          })
          .finally(_ => this.isLoading = false);
      })
    },
    setFilter: function(v) {
      this.filter.Filters = v;
      this.filter.Page = 1;
    },
    setSort: function(v) {
      this.filter.Sorts = v;
    },
    setPage: function(v) {
      this.filter.Page = v;
      this.load();
    },
    setLength: function(v) {
      this.filter.Page = 1;
      this.filter.Length = v;
      this.load();
    },
  },
});

if (import.meta.hot) {
  import.meta.hot.accept(acceptHMRUpdate(useGridUrl, import.meta.hot));
}