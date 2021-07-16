import { Tools } from './tools';
declare let Swal: any;

export class LoginTools {
  public static async loginBegin(username: string, password: string) {
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
      Tools.showErrorAlert('Request to server failed', e);
    }

    console.log('Assertion Options Object', makeAssertionOptions);

    // show options error to user
    if (makeAssertionOptions.status !== 'ok') {
      console.log('Error creating assertion options');
      console.log(makeAssertionOptions.errorMessage);
      Tools.showErrorAlert(makeAssertionOptions.errorMessage);
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
      Tools.showErrorAlert(err.message ? err.message : err);
    }

    return credential;
  }

  public static async loginEnd(assertedCredential) {
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
      rawId: Tools.coerceToBase64Url(rawId),
      type: assertedCredential.type,
      extensions: assertedCredential.getClientExtensionResults(),
      response: {
        authenticatorData: Tools.coerceToBase64Url(authData),
        clientDataJson: Tools.coerceToBase64Url(clientDataJSON),
        signature: Tools.coerceToBase64Url(sig),
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
      Tools.showErrorAlert('Request to server failed', e);
      throw e;
    }

    console.log('Assertion Object', response);

    // show error
    if (response.status !== 'ok') {
      console.log('Error doing assertion');
      console.log(response.errorMessage);
      Tools.showErrorAlert(response.errorMessage);
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
}
