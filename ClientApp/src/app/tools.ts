declare let Swal: any;

export class Tools {
  // HELPERS

  public static detectFIDOSupport() {
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

  public static showErrorAlert(message, error: any = undefined) {
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

  public static coerceToArrayBuffer(thing, name: string = undefined) {
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

  public static coerceToBase64Url(thing) {
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

  public static value(selector) {
    var el = document.querySelector(selector);
    if (el.type === 'checkbox') {
      return el.checked;
    }
    return el.value;
  }
}
