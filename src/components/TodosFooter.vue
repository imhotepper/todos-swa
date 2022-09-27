<template>
 <footer class="info">
      <p v-show="!isAuthenticated"><a href=".auth/login/google">Google login</a></p>
      <p v-show="isAuthenticated"><a href=".auth/logout">Logout</a></p>
      <!-- Remove the below line ↓ -->
      <p>
        Template by
        <a href="http://sindresorhus.com">Sindre Sorhus</a>
      </p>
      <!-- Change this out with your name and url ↓ -->
      <p>
        Created by
        <a href="http://todomvc.com">you</a>
      </p>
      <p>
        Part of
        <a href="http://todomvc.com">TodoMVC</a>
      </p>
    </footer>
</template>
<script>
  export default {
    name: "TodosActions",
    data(){return {"isAuthenticated": false}},
    methods: {
      async checkLoggedIn(){
          var resp = await fetch("/.auth/me");
          var json = await resp.json();
          this.data =  json;
          const { clientPrincipal } = json;
          this.isAuthenticated = !!clientPrincipal;
      }
    },
    async created(){
      await this.checkLoggedIn(); 
    }
  }
  </script>