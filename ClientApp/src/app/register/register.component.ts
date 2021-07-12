import { Component, OnInit } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { UserService } from '../user.service';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css'],
})
export class RegisterComponent implements OnInit {
  public registerForm = this.formBuilder.group({
    username: 'alice',
    displayName: 'alice',
    password1: 'alice',
    password2: 'alice',
  });
  constructor(
    private userService: UserService,
    private formBuilder: FormBuilder,
    private router:Router
  ) {}

  ngOnInit(): void {}

  register() {
    this.userService.register(this.registerForm).subscribe(success=>{this.router.navigateByUrl('/login')});
  }
}
