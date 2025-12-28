import Swal from "sweetalert2/dist/sweetalert2.js";
import "sweetalert2/src/sweetalert2.scss";

export default defineNuxtPlugin((nuxtApp) => {
  nuxtApp.provide("swal", {
    success: function (title, message) {
      Swal.fire({
        title: title || "Success!",
        text: message,
        icon: "success",
      });
    },
    error: function (title, message) {
      Swal.fire({
        title: title || "Error!",
        text: message,
        icon: "error",
      });
    },
    confirm: function (title, message) {
      return new Promise((resolve, reject) => {
        Swal.fire({
          title: title || "Confirm",
          text: message || "Are you sure want to perform this actions?",
          showCancelButton: true,
          confirmButtonText: "Yes",
        }).then((result) => {
          if (result.isConfirmed) {
            Swal.fire("Action executed!", "", "success");
            resolve();
          } else if (result.isDismissed) {
            Swal.fire("Cancelled", "Action was cancelled", "info");
            reject();
          }
        });
      });
    },
    confirmDelete: function (message) {
      return new Promise((resolve, reject) => {
        Swal.fire({
          title: "Delete Data",
          text: message || `Are you sure want to delete the selected data?`,
          showCancelButton: true,
          confirmButtonText: "Yes, delete it!",
        }).then((result) => {
          if (result.isConfirmed) {
            Swal.fire(
              "Deleted!",
              "Delete selected data successful!",
              "success"
            );
            resolve();
          } else if (result.isDismissed) {
            Swal.fire("Canceled", "Your data is safe.", "info");
            reject();
          }
        });
      });
    },
    confirmLockEss: function (message) {
      return new Promise((resolve, reject) => {
        Swal.fire({
          title: "Lock ESS",
          text: message || `Are you sure want to lock ESS the selected data?`,
          showCancelButton: true,
          confirmButtonText: "Yes, Lock it!",
        }).then((result) => {
          if (result.isConfirmed) {
            Swal.fire(
              "Lock!",
              "Lock selected data successful!",
              "success"
            );
            resolve();
          } else if (result.isDismissed) {
            Swal.fire("Canceled", "Your data is safe.", "info");
            reject();
          }
        });
      });
    },
    confirmCancel: function (message) {
      return new Promise((resolve, reject) => {
        Swal.fire({
          title: "Cancel Data",
          text: message || `Are you sure want to cancel the selected data?`,
          showCancelButton: true,
          confirmButtonText: "Yes, cancel it!",
        }).then((result) => {
          if (result.isConfirmed) {
            Swal.fire(
              "Canceled!",
              "Canceled selected data successful!",
              "success"
            );
            resolve();
          } else if (result.isDismissed) {
            Swal.fire("Canceled", "Your data is safe.", "info");
            reject();
          }
        });
      });
    },
    info: function (title, message) {
      Swal.fire({
        title: title || "Info!",
        text: message,
        icon: "info",
      });
    },
  });
});
