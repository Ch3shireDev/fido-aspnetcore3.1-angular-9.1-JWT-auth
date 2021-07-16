import { Component } from '@angular/core';
import { UserService } from './user.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'],
})
export class AppComponent {
  title = 'ClientApp';

  get user(): { displayName: string } {
    try{
    return this.userService.user;
    }
    catch{
      return null;
    }
  }

  constructor(private userService: UserService) {}

  ngOnInit() {}
}
