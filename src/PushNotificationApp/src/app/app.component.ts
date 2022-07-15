import { Component } from '@angular/core';
import {
  Plugins,
  PushNotification,
  PushNotificationToken,
  PushNotificationActionPerformed
} from '@capacitor/core';
import { AlertController, Platform } from '@ionic/angular';
import { HttpProviderService } from './Service/http-provider.service';
const { PushNotifications } = Plugins;

@Component({
  selector: 'app-root',
  templateUrl: 'app.component.html',
  styleUrls: ['app.component.scss'],
})
export class AppComponent {

  notificationDetail = {
    title: '',
    body: ''
  };

  constructor(private alertCtrl: AlertController, private httpProvider: HttpProviderService, private platform: Platform) { }

  ngOnInit() {
    PushNotifications.requestPermission().then(result => {
      if (result.granted) {
        PushNotifications.register();
      } else {
      }
    });
    PushNotifications.addListener('registration', (token: PushNotificationToken) => {
      let registrationToken = token.value;
      if (registrationToken != null && registrationToken != 'null') {
        localStorage.setItem("registrationToken", registrationToken);
        var DeviceType = 0;
        if (this.platform.is("android")) {
          DeviceType = 1;
        }
        else if (this.platform.is("ios")) {
          DeviceType = 2;
        }

        var pushNotificationsDetail = {
          DeviceId: registrationToken,
          DeviceType: DeviceType
        }

        this.httpProvider.addPushNotification(pushNotificationsDetail).subscribe(async data => {
          if (data != null && data.body != null && data.body.isSuccess == true) {
            localStorage.setItem("IsSuccessPushRegistration", "true");
            this.notificationDetail.title = "Success";
            this.notificationDetail.body = data.body.message;
            this.showPushNotificationAlert(this.notificationDetail);
          }
        },async error => {});
      }
    },
    );

    PushNotifications.addListener('registrationError', (error: any) => {
    });

    PushNotifications.addListener('pushNotificationReceived', (notification: PushNotification) => {
      this.showPushNotificationAlert(notification);
    });

    PushNotifications.addListener('pushNotificationActionPerformed', (args: PushNotificationActionPerformed) => {
    });
  }

  async showPushNotificationAlert(notificationDetail: any) {
    const alert = await this.alertCtrl.create({
      header: notificationDetail.title,
      message: notificationDetail.body,
      buttons: ['OK']
    });
    await alert.present();
  }
}
