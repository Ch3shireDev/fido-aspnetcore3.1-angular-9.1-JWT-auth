import { SecretService } from './../secret.service';
import { Component, OnInit } from '@angular/core';
import { UserService } from '../user.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-secret',
  templateUrl: './secret.component.html',
  styleUrls: ['./secret.component.css'],
})
export class SecretComponent implements OnInit {
  public secret: string;
  public error: string;

  constructor(
    private secretService: SecretService,
    private userService: UserService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.secretService.getSecret().subscribe(
      (result: { secret: string }) => {
        this.secret = result.secret;
      },
      (error) => {
        this.error = error.message;
        this.userService.logout();
        this.router.navigateByUrl('/login');
      }
    );
  }
}
