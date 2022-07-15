import { Component, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AlertController, Platform } from '@ionic/angular';
import { HttpProviderService } from '../Service/http-provider.service';

@Component({
  selector: 'app-home',
  templateUrl: 'home.page.html',
  styleUrls: ['home.page.scss'],
})
export class HomePage {

  notificationList = {
    NotificationTitle: '',
    NotificationBody:''
  };

  notificationDetail = {
    title: '',
    body: ''
  };

  @ViewChild('sendForm', { static: false }) sendForm: NgForm;

  constructor(private router: Router, private route: ActivatedRoute, public alertController: AlertController,
    private httpProvider: HttpProviderService, private platform : Platform) {
    this.route.queryParams.subscribe(params => {
      if (this.router.getCurrentNavigation() && this.router.getCurrentNavigation().extras &&
        this.router.getCurrentNavigation().extras.state) {
        let data = this.router.getCurrentNavigation().extras.state;
        this.showPushNotificationAlert(data);
      }
    });
  }

  async showPushNotificationAlert(notificationDetail: any) {
    const alert = await this.alertController.create({
      header: notificationDetail.title,
      message: notificationDetail.body,
      buttons: ['OK']
    });
    await alert.present();
  }

  sendMessage(form: NgForm) {
    if (form.valid) {
      this.httpProvider.sendPushNotification(this.notificationList).subscribe(async data => {
        if (data != null && data.body != null && data.body.isSuccess == true) {
          this.notificationDetail.title = "Success";          
          this.notificationDetail.body = data.body.message;
          this.showPushNotificationAlert(this.notificationDetail);
        }
      },async error => {});
    }
    else {
      this.notificationDetail.title = "Warning";
      this.notificationDetail.body = "Please fill the message";
      this.showPushNotificationAlert(this.notificationDetail);
    }
  }
}
