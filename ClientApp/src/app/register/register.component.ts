import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { Router } from '@angular/router';
import { UserService } from '../user.service';

declare let Swal: any;
@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css'],
})
export class RegisterComponent implements OnInit {
  public registerForm = this.formBuilder.group({
    username: 'alice',
    displayName: 'alice',
    password: 'alice',
  });
  constructor(
    private userService: UserService,
    private formBuilder: FormBuilder,
    private router: Router,
    private http: HttpClient
  ) {}

  ngOnInit(): void {}
  register() {
    // this.userService.register(this.registerForm).subscribe((success) => {
    //   this.router.navigateByUrl('/login');
    // });

    let username = this.registerForm.get('username').value;
    let displayName = this.registerForm.get('displayName').value;
    let password = this.registerForm.get('password').value;

    this.userService.register(username, displayName, password).then(() => {
      this.router.navigateByUrl('/login');
    });
  }
}
