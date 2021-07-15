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
  async register() {
    // this.userService.register(this.registerForm).subscribe((success) => {
    //   this.router.navigateByUrl('/login');
    // });

    let username = this.registerForm.get('username').value;
    let displayName = this.registerForm.get('displayName').value;
    let password = this.registerForm.get('password').value;

    await this.handleRegisterSubmit(username, displayName, password);
  }

  public async handleRegisterSubmit(
    username: string,
    displayName: string,
    password: string
  ) {
    // passwordfield is omitted in demo
    // let password = this.password.value;

    // possible values: none, direct, indirect
    let attestation_type = 'none';
    // possible values: <empty>, platform, cross-platform
    let authenticator_attachment = '';

    // possible values: preferred, required, discouraged
    let user_verification = 'preferred';

    // possible values: true,false
    let require_resident_key = false;

    // prepare form post data
    var data = new FormData();
    data.append('username', username);
    data.append('displayName', displayName);
    data.append('password', password);
    data.append('attType', attestation_type);
    data.append('authType', authenticator_attachment);
    data.append('userVerification', user_verification);
    data.append('requireResidentKey', JSON.stringify(require_resident_key));

    console.log(password);

    // send to server for registering
    let makeCredentialOptions;
    try {
      makeCredentialOptions = await this.fetchMakeCredentialOptions(data);
    } catch (e) {
      console.error(e);
      let msg = "Something wen't really wrong";
      this.showErrorAlert(msg);
    }

    console.log('Credential Options Object', makeCredentialOptions);

    if (makeCredentialOptions.status !== 'ok') {
      console.log('Error creating credential options');
      console.log(makeCredentialOptions.errorMessage);
      this.showErrorAlert(makeCredentialOptions.errorMessage);
      return;
    }

    // Turn the challenge back into the accepted format of padded base64
    makeCredentialOptions.challenge = this.coerceToArrayBuffer(
      makeCredentialOptions.challenge
    );
    // Turn ID into a UInt8Array Buffer for some reason
    makeCredentialOptions.user.id = this.coerceToArrayBuffer(
      makeCredentialOptions.user.id
    );

    makeCredentialOptions.excludeCredentials =
      makeCredentialOptions.excludeCredentials.map((c) => {
        c.id = this.coerceToArrayBuffer(c.id);
        return c;
      });

    if (
      makeCredentialOptions.authenticatorSelection.authenticatorAttachment ===
      null
    )
      makeCredentialOptions.authenticatorSelection.authenticatorAttachment =
        undefined;

    console.log('Credential Options Formatted', makeCredentialOptions);

    Swal.fire({
      title: 'Registering...',
      text: 'Tap your security key to finish registration.',
      imageUrl: '/images/securitykey.min.svg',
      showCancelButton: true,
      showConfirmButton: false,
      focusConfirm: false,
      focusCancel: false,
    });

    console.log('Creating PublicKeyCredential...');

    let newCredential;

    console.log(navigator);

    try {
      newCredential = await navigator.credentials.create({
        publicKey: makeCredentialOptions,
      });
    } catch (e) {
      var msg =
        'Could not create credentials in browser. Probably because the username is already registered with your authenticator. Please change username or authenticator.';
      console.error(msg, e);
      this.showErrorAlert(msg, e);
    }

    console.log('PublicKeyCredential Created', newCredential);

    try {
      this.registerNewCredential(newCredential);
    } catch (err) {
      this.showErrorAlert(err.message ? err.message : err);
    }
  }

  async fetchMakeCredentialOptions(formData) {
    let response = await fetch('/api/user/register-begin', {
      method: 'POST', // or 'PUT'
      body: formData, // data can be `string` or {object}!
      headers: {
        Accept: 'application/json',
      },
    });

    let data = await response.json();

    return data;
  }

  // This should be used to verify the auth data with the server
  async registerNewCredential(newCredential) {
    // Move data into Arrays incase it is super long
    let attestationObject = new Uint8Array(
      newCredential.response.attestationObject
    );
    let clientDataJSON = new Uint8Array(newCredential.response.clientDataJSON);
    let rawId = new Uint8Array(newCredential.rawId);

    const data = {
      id: newCredential.id,
      rawId: this.coerceToBase64Url(rawId),
      type: newCredential.type,
      extensions: newCredential.getClientExtensionResults(),
      response: {
        AttestationObject: this.coerceToBase64Url(attestationObject),
        clientDataJson: this.coerceToBase64Url(clientDataJSON),
      },
    };

    let response;
    try {
      response = await this.registerCredentialWithServer(data);
    } catch (e) {
      this.showErrorAlert(e);
    }

    console.log('Credential Object', response);

    // show error
    if (response.status !== 'ok') {
      console.log('Error creating credential');
      console.log(response.errorMessage);
      this.showErrorAlert(response.errorMessage);
      return;
    }

    // show success
    Swal.fire({
      title: 'Registration Successful!',
      text: "You've registered successfully.",
      type: 'success',
      timer: 2000,
    });

    // redirect to dashboard?
    //window.location.href = "/dashboard/" + state.user.displayName;
  }

  async registerCredentialWithServer(formData) {
    let response = await fetch('/api/user/register-end', {
      method: 'POST', // or 'PUT'
      body: JSON.stringify(formData), // data can be `string` or {object}!
      headers: {
        Accept: 'application/json',
        'Content-Type': 'application/json',
      },
    });

    let data = await response.json();

    return data;
  }

  // HELPERS

  public showErrorAlert(message, error: any = undefined) {
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

  public detectFIDOSupport() {
    if (
      window.PublicKeyCredential === undefined ||
      typeof window.PublicKeyCredential !== 'function'
    ) {
      var el = document.getElementById('notSupportedWarning');
      if (el) {
        el.style.display = 'block';
      }
      return;
    }
  }

  public value(selector) {
    var el = document.querySelector(selector);
    if (el.type === 'checkbox') {
      return el.checked;
    }
    return el.value;
  }

  coerceToArrayBuffer(thing, name: string = undefined) {
    if (typeof thing === 'string') {
      // base64url to base64
      thing = thing.replace(/-/g, '+').replace(/_/g, '/');

      // base64 to Uint8Array
      var str = window.atob(thing);
      var bytes = new Uint8Array(str.length);
      for (var i = 0; i < str.length; i++) {
        bytes[i] = str.charCodeAt(i);
      }
      thing = bytes;
    }

    // Array to Uint8Array
    if (Array.isArray(thing)) {
      thing = new Uint8Array(thing);
    }

    // Uint8Array to ArrayBuffer
    if (thing instanceof Uint8Array) {
      thing = thing.buffer;
    }

    // error if none of the above worked
    if (!(thing instanceof ArrayBuffer)) {
      throw new TypeError("could not coerce '" + name + "' to ArrayBuffer");
    }

    return thing;
  }

  coerceToBase64Url(thing) {
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
  }
}
