import { Tools } from './tools';
declare let Swal: any;

export class RegisterTools {
  public static async registerBegin(
    username: string,
    displayName: string,
    password: string
  ) {
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

    // send to server for registering
    let makeCredentialOptions;
    try {
      let response = await fetch('/api/user/register-begin', {
        method: 'POST', // or 'PUT'
        body: data, // data can be `string` or {object}!
        headers: {
          Accept: 'application/json',
        },
      });

      makeCredentialOptions = await response.json();
    } catch (e) {
      console.error(e);
      let msg = "Something wen't really wrong";
      Tools.showErrorAlert(msg);
    }

    console.log('Credential Options Object', makeCredentialOptions);

    if (makeCredentialOptions.status !== 'ok') {
      console.log('Error creating credential options');
      console.log(makeCredentialOptions.errorMessage);
      Tools.showErrorAlert(makeCredentialOptions.errorMessage);
      return;
    }

    // Turn the challenge back into the accepted format of padded base64
    makeCredentialOptions.challenge = Tools.coerceToArrayBuffer(
      makeCredentialOptions.challenge
    );
    // Turn ID into a UInt8Array Buffer for some reason
    makeCredentialOptions.user.id = Tools.coerceToArrayBuffer(
      makeCredentialOptions.user.id
    );

    makeCredentialOptions.excludeCredentials =
      makeCredentialOptions.excludeCredentials.map((c) => {
        c.id = Tools.coerceToArrayBuffer(c.id);
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
      Tools.showErrorAlert(msg, e);
    }

    console.log('PublicKeyCredential Created', newCredential);

    return newCredential;
  }

  // Weryfikujemy dane autentykacyjne z serwerem.
  public static async registerEnd(newCredential) {
    // Move data into Arrays incase it is super long
    let attestationObject = new Uint8Array(
      newCredential.response.attestationObject
    );
    let clientDataJSON = new Uint8Array(newCredential.response.clientDataJSON);
    let rawId = new Uint8Array(newCredential.rawId);

    const data = {
      id: newCredential.id,
      rawId: Tools.coerceToBase64Url(rawId),
      type: newCredential.type,
      extensions: newCredential.getClientExtensionResults(),
      response: {
        AttestationObject: Tools.coerceToBase64Url(attestationObject),
        clientDataJson: Tools.coerceToBase64Url(clientDataJSON),
      },
    };

    let response;
    try {
      // response = await this.registerCredentialWithServer(data);
      let response2 = await fetch('/api/user/register-end', {
        method: 'POST', // or 'PUT'
        body: JSON.stringify(data), // data can be `string` or {object}!
        headers: {
          Accept: 'application/json',
          'Content-Type': 'application/json',
        },
      });
      response = await response2.json();
    } catch (e) {
      Tools.showErrorAlert(e);
    }

    console.log('Credential Object', response);

    // show error
    if (response.status !== 'ok') {
      console.log('Error creating credential');
      console.log(response.errorMessage);
      Tools.showErrorAlert(response.errorMessage);
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
}
