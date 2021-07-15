import { FormBuilder } from '@angular/forms';
import { Component, OnInit } from '@angular/core';
import { UserService } from '../user.service';
import { Router } from '@angular/router';

declare let Swal: any;

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
    let username = this.loginForm.get('username').value;
    let password = this.loginForm.get('username').value;
    this.handleSignInSubmit(username, password);
  }

  public async handleSignInSubmit(username: string, password: string) {
    // prepare form post data
    var formData = new FormData();
    formData.append('username', username);
    formData.append('password', password);

    // not done in demo
    // todo: validate username + password with server (has nothing to do with FIDO2/WebAuthn)

    // send to server for registering
    let makeAssertionOptions;
    try {
      var res = await fetch('/api/user/login-begin', {
        method: 'POST', // or 'PUT'
        body: formData, // data can be `string` or {object}!
        headers: {
          Accept: 'application/json',
        },
      });

      makeAssertionOptions = await res.json();
    } catch (e) {
      this.showErrorAlert('Request to server failed', e);
    }

    console.log('Assertion Options Object', makeAssertionOptions);

    // show options error to user
    if (makeAssertionOptions.status !== 'ok') {
      console.log('Error creating assertion options');
      console.log(makeAssertionOptions.errorMessage);
      this.showErrorAlert(makeAssertionOptions.errorMessage);
      return;
    }

    // todo: switch this to coercebase64
    const challenge = makeAssertionOptions.challenge
      .replace(/-/g, '+')
      .replace(/_/g, '/');
    makeAssertionOptions.challenge = Uint8Array.from(atob(challenge), (c) =>
      c.charCodeAt(0)
    );

    // fix escaping. Change this to coerce
    makeAssertionOptions.allowCredentials.forEach(function (listItem) {
      var fixedId = listItem.id.replace(/\_/g, '/').replace(/\-/g, '+');
      listItem.id = Uint8Array.from(atob(fixedId), (c) => c.charCodeAt(0));
    });

    console.log('Assertion options', makeAssertionOptions);

    Swal.fire({
      title: 'Logging In...',
      text: 'Tap your security key to login.',
      imageUrl: '/assets/securitykey.min.svg',
      showCancelButton: true,
      showConfirmButton: false,
      focusConfirm: false,
      focusCancel: false,
    });

    // ask browser for credentials (browser will ask connected authenticators)
    let credential;
    try {
      credential = await navigator.credentials.get({
        publicKey: makeAssertionOptions,
      });
    } catch (err) {
      this.showErrorAlert(err.message ? err.message : err);
    }

    try {
      await this.verifyAssertionWithServer(credential);
    } catch (e) {
      this.showErrorAlert('Could not verify assertion', e);
    }
  }

  public async verifyAssertionWithServer(assertedCredential) {
    // Move data into Arrays incase it is super long
    let authData = new Uint8Array(
      assertedCredential.response.authenticatorData
    );
    let clientDataJSON = new Uint8Array(
      assertedCredential.response.clientDataJSON
    );
    let rawId = new Uint8Array(assertedCredential.rawId);
    let sig = new Uint8Array(assertedCredential.response.signature);
    const data = {
      id: assertedCredential.id,
      rawId: this.coerceToBase64Url(rawId),
      type: assertedCredential.type,
      extensions: assertedCredential.getClientExtensionResults(),
      response: {
        authenticatorData: this.coerceToBase64Url(authData),
        clientDataJson: this.coerceToBase64Url(clientDataJSON),
        signature: this.coerceToBase64Url(sig),
      },
    };

    let response;
    try {
      let res = await fetch('/api/user/login-end', {
        method: 'POST', // or 'PUT'
        body: JSON.stringify(data), // data can be `string` or {object}!
        headers: {
          Accept: 'application/json',
          'Content-Type': 'application/json',
        },
      });

      response = await res.json();
    } catch (e) {
      this.showErrorAlert('Request to server failed', e);
      throw e;
    }

    console.log('Assertion Object', response);

    // show error
    if (response.status !== 'ok') {
      console.log('Error doing assertion');
      console.log(response.errorMessage);
      this.showErrorAlert(response.errorMessage);
      return;
    }

    console.log(response);

    // show success message
    await Swal.fire({
      title: 'Logged In!',
      text: "You're logged in successfully.",
      type: 'success',
      timer: 2000,
    });
  }

  public value(selector) {
    var el = document.querySelector(selector);
    if (el.type === 'checkbox') {
      return el.checked;
    }
    return el.value;
  }

  public showErrorAlert(message, error = undefined) {
    let footermsg = '';
    if (error) {
      footermsg = 'exception:' + error.toString();
    }
    Swal.fire({
      type: 'error',
      title: 'Error',
      text: message,
      footer: footermsg,
      //footer: '<a href>Why do I have this issue?</a>'
    });
  }

  coerceToBase64Url = function (thing) {
    // Array or ArrayBuffer to Uint8Array
    if (Array.isArray(thing)) {
      thing = Uint8Array.from(thing);
    }

    if (thing instanceof ArrayBuffer) {
      thing = new Uint8Array(thing);
    }

    // Uint8Array to base64
    if (thing instanceof Uint8Array) {
      var str = '';
      var len = thing.byteLength;

      for (var i = 0; i < len; i++) {
        str += String.fromCharCode(thing[i]);
      }
      thing = window.btoa(str);
    }

    if (typeof thing !== 'string') {
      throw new Error('could not coerce to string');
    }

    // base64 to base64url
    // NOTE: "=" at the end of challenge is optional, strip it off here
    thing = thing.replace(/\+/g, '-').replace(/\//g, '_').replace(/=*$/g, '');

    return thing;
  };
}
