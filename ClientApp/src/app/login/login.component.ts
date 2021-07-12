import { FormBuilder } from '@angular/forms';
import { Component, OnInit } from '@angular/core';
import { UserService } from '../user.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
})
export class LoginComponent implements OnInit {
  loginForm = this.formBuilder.group({
    username: 'alice',
    password: 'alice',
  });

  constructor(
    private userService: UserService,
    private formBuilder: FormBuilder,
    private router: Router
  ) {}

  ngOnInit(): void {}

  public login() {
    console.log(this.loginForm)
    this.userService.login(this.loginForm).subscribe(
      (result) => {
        this.router.navigateByUrl('secret');
      },
      (error) => console.log(error)
    );
  }
}
