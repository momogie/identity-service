
var app = useNuxtApp();

export const useUser = defineStore('User', {
  state: () => ({
    isLoading: false,
    isSigningIn: false,
    isSigningUp: false,
    isLoadingEmployeeProfile: false,
    isEmployeeProfileLoaded: false,
    isServerError: false,
    isNetworkError: false,
    data: {
      Name: null,
      Email: null,
      UserName: null,
      EmailConfirmed: false,
    },
    scopes: [],
    currentOrganization: {},
    organizationList: [],
  }),
  actions: {
    load: function() {
      return new Promise((resolve, reject) => {
        app.$http.get('/profile/user-info')
          .then(({data}) => {
            this.data = data.Data;
          })
        resolve(data);
      })
    },
  },
});

if (import.meta.hot) {
  import.meta.hot.accept(acceptHMRUpdate(useUser, import.meta.hot));
}