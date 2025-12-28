import { useUser } from "~/stores/user"

export default defineNuxtRouteMiddleware((to, from) => {
  var user = useUser();
  var app = useApp();
  user.load().then((e) => {
    // if(to.path == '/')
    //   location.href = '/app'
  })
  .catch((err) => {
    // location.href = '/auth/sign-in'
  });
  
  var theme = useTheme();
  theme.load();
})