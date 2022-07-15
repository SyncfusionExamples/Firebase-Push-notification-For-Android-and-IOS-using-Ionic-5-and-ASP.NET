import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { WebApiService } from './web-api.service';

var apiUrl = "https://pushnotificationwebapiapplication.azurewebsites.net/";
//var apiUrl = "https://localhost:44370/";

var httpLink = {
  addPushNotification: apiUrl + "api/pushNotification/addPushNotification",
  sendPushNotification: apiUrl + "api/pushNotification/sendPushNotification"
}
@Injectable({
  providedIn: 'root'
})
export class HttpProviderService {
  constructor(private webApiService: WebApiService) { }

  public addPushNotification(pushNotificationsDetail: any): Observable<any> {
    return this.webApiService.post(httpLink.addPushNotification , pushNotificationsDetail);
  }
  public sendPushNotification(pushNotificationsDetail: any): Observable<any> {
    return this.webApiService.post(httpLink.sendPushNotification,pushNotificationsDetail);
  }
}