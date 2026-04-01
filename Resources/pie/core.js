if(typeof process==="undefined"){globalThis.process={env:{},versions:{},version:"0.0.0",platform:"neutral",arch:"x64"};}
if(typeof fetch==="undefined"){
  globalThis.fetch=function fetch(){return Promise.reject(new Error("[pie] fetch stub: use UnityHttpClient instead"));};
  globalThis.Response=function Response(body,init){this.ok=true;this.status=200;this.headers=new Headers();};
  globalThis.Request=function Request(input,init){this.url=input;this.method=(init&&init.method)||"GET";};
  globalThis.Headers=function Headers(init){var h={};this.append=function(k,v){h[k.toLowerCase()]=v;};this.get=function(k){return h[k.toLowerCase()]||null;};this.set=function(k,v){h[k.toLowerCase()]=v;};this.has=function(k){return k.toLowerCase() in h;};this.delete=function(k){delete h[k.toLowerCase()];};this.forEach=function(fn){Object.keys(h).forEach(function(k){fn(h[k],k);});};};
}
if(typeof Buffer==="undefined"){
  globalThis.Buffer={
    from:function(str,enc){if(typeof str==="string"){var s=enc==="utf-8"||enc==="utf8"||!enc?str:str;var n=0;for(var i=0;i<s.length;i++){var c=s.charCodeAt(i);if(c<0x80)n++;else if(c<0x800)n+=2;else if(c>=0xD800&&c<=0xDBFF&&i+1<s.length){n+=4;i++;}else n+=3;}return{length:n,toString:function(){return str;}};}return{length:str.length||0,toString:function(){return String(str);}};},
    byteLength:function(str,enc){return Buffer.from(str,enc).length;},
    isBuffer:function(obj){return false;},
    concat:function(list){return{length:list.reduce(function(a,b){return a+(b.length||0);},0)};},
    alloc:function(size){return{length:size};}
  };
}
if(typeof AbortController==="undefined"){
  globalThis.AbortSignal=function AbortSignal(){this.aborted=false;this.reason=undefined;this._listeners=[];this.onabort=null;};
  globalThis.AbortSignal.prototype.addEventListener=function(t,fn){if(t==="abort")this._listeners.push(fn);};
  globalThis.AbortSignal.prototype.removeEventListener=function(t,fn){this._listeners=this._listeners.filter(function(l){return l!==fn;});};
  globalThis.AbortSignal.prototype.throwIfAborted=function(){if(this.aborted){var e=new Error("This operation was aborted");e.name="AbortError";throw e;}};
  globalThis.AbortController=function AbortController(){this.signal=new AbortSignal();};
  globalThis.AbortController.prototype.abort=function(reason){if(this.signal.aborted)return;this.signal.aborted=true;this.signal.reason=reason||"AbortError";if(this.signal.onabort)this.signal.onabort({type:"abort"});this.signal._listeners.forEach(function(fn){fn({type:"abort"});});};
}
"use strict";
var _PieAgent = (() => {
  var __create = Object.create;
  var __defProp = Object.defineProperty;
  var __getOwnPropDesc = Object.getOwnPropertyDescriptor;
  var __getOwnPropNames = Object.getOwnPropertyNames;
  var __getProtoOf = Object.getPrototypeOf;
  var __hasOwnProp = Object.prototype.hasOwnProperty;
  var __require = /* @__PURE__ */ ((x) => typeof require !== "undefined" ? require : typeof Proxy !== "undefined" ? new Proxy(x, {
    get: (a, b) => (typeof require !== "undefined" ? require : a)[b]
  }) : x)(function(x) {
    if (typeof require !== "undefined") return require.apply(this, arguments);
    throw Error('Dynamic require of "' + x + '" is not supported');
  });
  var __commonJS = (cb, mod) => function __require2() {
    return mod || (0, cb[__getOwnPropNames(cb)[0]])((mod = { exports: {} }).exports, mod), mod.exports;
  };
  var __export = (target, all) => {
    for (var name in all)
      __defProp(target, name, { get: all[name], enumerable: true });
  };
  var __copyProps = (to, from, except, desc) => {
    if (from && typeof from === "object" || typeof from === "function") {
      for (let key of __getOwnPropNames(from))
        if (!__hasOwnProp.call(to, key) && key !== except)
          __defProp(to, key, { get: () => from[key], enumerable: !(desc = __getOwnPropDesc(from, key)) || desc.enumerable });
    }
    return to;
  };
  var __toESM = (mod, isNodeMode, target) => (target = mod != null ? __create(__getProtoOf(mod)) : {}, __copyProps(
    // If the importer is in node compatibility mode or this is not an ESM
    // file that has been converted to a CommonJS file using a Babel-
    // compatible transform (i.e. "__esModule" has not been set), then set
    // "default" to the CommonJS "module.exports" for node compatibility.
    isNodeMode || !mod || !mod.__esModule ? __defProp(target, "default", { value: mod, enumerable: true }) : target,
    mod
  ));

  // ../../node_modules/partial-json/dist/options.js
  var require_options = __commonJS({
    "../../node_modules/partial-json/dist/options.js"(exports) {
      "use strict";
      Object.defineProperty(exports, "__esModule", { value: true });
      exports.Allow = exports.ALL = exports.COLLECTION = exports.ATOM = exports.SPECIAL = exports.INF = exports._INFINITY = exports.INFINITY = exports.NAN = exports.BOOL = exports.NULL = exports.OBJ = exports.ARR = exports.NUM = exports.STR = void 0;
      exports.STR = 1;
      exports.NUM = 2;
      exports.ARR = 4;
      exports.OBJ = 8;
      exports.NULL = 16;
      exports.BOOL = 32;
      exports.NAN = 64;
      exports.INFINITY = 128;
      exports._INFINITY = 256;
      exports.INF = exports.INFINITY | exports._INFINITY;
      exports.SPECIAL = exports.NULL | exports.BOOL | exports.INF | exports.NAN;
      exports.ATOM = exports.STR | exports.NUM | exports.SPECIAL;
      exports.COLLECTION = exports.ARR | exports.OBJ;
      exports.ALL = exports.ATOM | exports.COLLECTION;
      exports.Allow = { STR: exports.STR, NUM: exports.NUM, ARR: exports.ARR, OBJ: exports.OBJ, NULL: exports.NULL, BOOL: exports.BOOL, NAN: exports.NAN, INFINITY: exports.INFINITY, _INFINITY: exports._INFINITY, INF: exports.INF, SPECIAL: exports.SPECIAL, ATOM: exports.ATOM, COLLECTION: exports.COLLECTION, ALL: exports.ALL };
      exports.default = exports.Allow;
    }
  });

  // ../../node_modules/partial-json/dist/index.js
  var require_dist = __commonJS({
    "../../node_modules/partial-json/dist/index.js"(exports) {
      "use strict";
      var __createBinding = exports && exports.__createBinding || (Object.create ? function(o, m, k, k2) {
        if (k2 === void 0) k2 = k;
        var desc = Object.getOwnPropertyDescriptor(m, k);
        if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
          desc = { enumerable: true, get: function() {
            return m[k];
          } };
        }
        Object.defineProperty(o, k2, desc);
      } : function(o, m, k, k2) {
        if (k2 === void 0) k2 = k;
        o[k2] = m[k];
      });
      var __exportStar = exports && exports.__exportStar || function(m, exports2) {
        for (var p in m) if (p !== "default" && !Object.prototype.hasOwnProperty.call(exports2, p)) __createBinding(exports2, m, p);
      };
      Object.defineProperty(exports, "__esModule", { value: true });
      exports.Allow = exports.MalformedJSON = exports.PartialJSON = exports.parseJSON = exports.parse = void 0;
      var options_1 = require_options();
      Object.defineProperty(exports, "Allow", { enumerable: true, get: function() {
        return options_1.Allow;
      } });
      __exportStar(require_options(), exports);
      var PartialJSON = class extends Error {
      };
      exports.PartialJSON = PartialJSON;
      var MalformedJSON = class extends Error {
      };
      exports.MalformedJSON = MalformedJSON;
      function parseJSON(jsonString, allowPartial = options_1.Allow.ALL) {
        if (typeof jsonString !== "string") {
          throw new TypeError(`expecting str, got ${typeof jsonString}`);
        }
        if (!jsonString.trim()) {
          throw new Error(`${jsonString} is empty`);
        }
        return _parseJSON(jsonString.trim(), allowPartial);
      }
      exports.parseJSON = parseJSON;
      var _parseJSON = (jsonString, allow) => {
        const length = jsonString.length;
        let index = 0;
        const markPartialJSON = (msg) => {
          throw new PartialJSON(`${msg} at position ${index}`);
        };
        const throwMalformedError = (msg) => {
          throw new MalformedJSON(`${msg} at position ${index}`);
        };
        const parseAny = () => {
          skipBlank();
          if (index >= length)
            markPartialJSON("Unexpected end of input");
          if (jsonString[index] === '"')
            return parseStr();
          if (jsonString[index] === "{")
            return parseObj();
          if (jsonString[index] === "[")
            return parseArr();
          if (jsonString.substring(index, index + 4) === "null" || options_1.Allow.NULL & allow && length - index < 4 && "null".startsWith(jsonString.substring(index))) {
            index += 4;
            return null;
          }
          if (jsonString.substring(index, index + 4) === "true" || options_1.Allow.BOOL & allow && length - index < 4 && "true".startsWith(jsonString.substring(index))) {
            index += 4;
            return true;
          }
          if (jsonString.substring(index, index + 5) === "false" || options_1.Allow.BOOL & allow && length - index < 5 && "false".startsWith(jsonString.substring(index))) {
            index += 5;
            return false;
          }
          if (jsonString.substring(index, index + 8) === "Infinity" || options_1.Allow.INFINITY & allow && length - index < 8 && "Infinity".startsWith(jsonString.substring(index))) {
            index += 8;
            return Infinity;
          }
          if (jsonString.substring(index, index + 9) === "-Infinity" || options_1.Allow._INFINITY & allow && 1 < length - index && length - index < 9 && "-Infinity".startsWith(jsonString.substring(index))) {
            index += 9;
            return -Infinity;
          }
          if (jsonString.substring(index, index + 3) === "NaN" || options_1.Allow.NAN & allow && length - index < 3 && "NaN".startsWith(jsonString.substring(index))) {
            index += 3;
            return NaN;
          }
          return parseNum();
        };
        const parseStr = () => {
          const start = index;
          let escape = false;
          index++;
          while (index < length && (jsonString[index] !== '"' || escape && jsonString[index - 1] === "\\")) {
            escape = jsonString[index] === "\\" ? !escape : false;
            index++;
          }
          if (jsonString.charAt(index) == '"') {
            try {
              return JSON.parse(jsonString.substring(start, ++index - Number(escape)));
            } catch (e) {
              throwMalformedError(String(e));
            }
          } else if (options_1.Allow.STR & allow) {
            try {
              return JSON.parse(jsonString.substring(start, index - Number(escape)) + '"');
            } catch (e) {
              return JSON.parse(jsonString.substring(start, jsonString.lastIndexOf("\\")) + '"');
            }
          }
          markPartialJSON("Unterminated string literal");
        };
        const parseObj = () => {
          index++;
          skipBlank();
          const obj = {};
          try {
            while (jsonString[index] !== "}") {
              skipBlank();
              if (index >= length && options_1.Allow.OBJ & allow)
                return obj;
              const key = parseStr();
              skipBlank();
              index++;
              try {
                const value = parseAny();
                obj[key] = value;
              } catch (e) {
                if (options_1.Allow.OBJ & allow)
                  return obj;
                else
                  throw e;
              }
              skipBlank();
              if (jsonString[index] === ",")
                index++;
            }
          } catch (e) {
            if (options_1.Allow.OBJ & allow)
              return obj;
            else
              markPartialJSON("Expected '}' at end of object");
          }
          index++;
          return obj;
        };
        const parseArr = () => {
          index++;
          const arr = [];
          try {
            while (jsonString[index] !== "]") {
              arr.push(parseAny());
              skipBlank();
              if (jsonString[index] === ",") {
                index++;
              }
            }
          } catch (e) {
            if (options_1.Allow.ARR & allow) {
              return arr;
            }
            markPartialJSON("Expected ']' at end of array");
          }
          index++;
          return arr;
        };
        const parseNum = () => {
          if (index === 0) {
            if (jsonString === "-")
              throwMalformedError("Not sure what '-' is");
            try {
              return JSON.parse(jsonString);
            } catch (e) {
              if (options_1.Allow.NUM & allow)
                try {
                  return JSON.parse(jsonString.substring(0, jsonString.lastIndexOf("e")));
                } catch (e2) {
                }
              throwMalformedError(String(e));
            }
          }
          const start = index;
          if (jsonString[index] === "-")
            index++;
          while (jsonString[index] && ",]}".indexOf(jsonString[index]) === -1)
            index++;
          if (index == length && !(options_1.Allow.NUM & allow))
            markPartialJSON("Unterminated number literal");
          try {
            return JSON.parse(jsonString.substring(start, index));
          } catch (e) {
            if (jsonString.substring(start, index) === "-")
              markPartialJSON("Not sure what '-' is");
            try {
              return JSON.parse(jsonString.substring(start, jsonString.lastIndexOf("e")));
            } catch (e2) {
              throwMalformedError(String(e2));
            }
          }
        };
        const skipBlank = () => {
          while (index < length && " \n\r	".includes(jsonString[index])) {
            index++;
          }
        };
        return parseAny();
      };
      var parse = parseJSON;
      exports.parse = parse;
    }
  });

  // ../../node_modules/@sinclair/typebox/build/esm/type/guard/value.mjs
  var value_exports = {};
  __export(value_exports, {
    HasPropertyKey: () => HasPropertyKey,
    IsArray: () => IsArray,
    IsAsyncIterator: () => IsAsyncIterator,
    IsBigInt: () => IsBigInt,
    IsBoolean: () => IsBoolean,
    IsDate: () => IsDate,
    IsFunction: () => IsFunction,
    IsIterator: () => IsIterator,
    IsNull: () => IsNull,
    IsNumber: () => IsNumber,
    IsObject: () => IsObject,
    IsRegExp: () => IsRegExp,
    IsString: () => IsString,
    IsSymbol: () => IsSymbol,
    IsUint8Array: () => IsUint8Array,
    IsUndefined: () => IsUndefined
  });
  function HasPropertyKey(value, key) {
    return key in value;
  }
  function IsAsyncIterator(value) {
    return IsObject(value) && !IsArray(value) && !IsUint8Array(value) && Symbol.asyncIterator in value;
  }
  function IsArray(value) {
    return Array.isArray(value);
  }
  function IsBigInt(value) {
    return typeof value === "bigint";
  }
  function IsBoolean(value) {
    return typeof value === "boolean";
  }
  function IsDate(value) {
    return value instanceof globalThis.Date;
  }
  function IsFunction(value) {
    return typeof value === "function";
  }
  function IsIterator(value) {
    return IsObject(value) && !IsArray(value) && !IsUint8Array(value) && Symbol.iterator in value;
  }
  function IsNull(value) {
    return value === null;
  }
  function IsNumber(value) {
    return typeof value === "number";
  }
  function IsObject(value) {
    return typeof value === "object" && value !== null;
  }
  function IsRegExp(value) {
    return value instanceof globalThis.RegExp;
  }
  function IsString(value) {
    return typeof value === "string";
  }
  function IsSymbol(value) {
    return typeof value === "symbol";
  }
  function IsUint8Array(value) {
    return value instanceof globalThis.Uint8Array;
  }
  function IsUndefined(value) {
    return value === void 0;
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/clone/value.mjs
  function ArrayType(value) {
    return value.map((value2) => Visit(value2));
  }
  function DateType(value) {
    return new Date(value.getTime());
  }
  function Uint8ArrayType(value) {
    return new Uint8Array(value);
  }
  function RegExpType(value) {
    return new RegExp(value.source, value.flags);
  }
  function ObjectType(value) {
    const result = {};
    for (const key of Object.getOwnPropertyNames(value)) {
      result[key] = Visit(value[key]);
    }
    for (const key of Object.getOwnPropertySymbols(value)) {
      result[key] = Visit(value[key]);
    }
    return result;
  }
  function Visit(value) {
    return IsArray(value) ? ArrayType(value) : IsDate(value) ? DateType(value) : IsUint8Array(value) ? Uint8ArrayType(value) : IsRegExp(value) ? RegExpType(value) : IsObject(value) ? ObjectType(value) : value;
  }
  function Clone(value) {
    return Visit(value);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/clone/type.mjs
  function CloneType(schema, options) {
    return options === void 0 ? Clone(schema) : Clone({ ...options, ...schema });
  }

  // ../../node_modules/@sinclair/typebox/build/esm/value/guard/guard.mjs
  function IsAsyncIterator2(value) {
    return IsObject2(value) && globalThis.Symbol.asyncIterator in value;
  }
  function IsIterator2(value) {
    return IsObject2(value) && globalThis.Symbol.iterator in value;
  }
  function IsStandardObject(value) {
    return IsObject2(value) && (globalThis.Object.getPrototypeOf(value) === Object.prototype || globalThis.Object.getPrototypeOf(value) === null);
  }
  function IsPromise(value) {
    return value instanceof globalThis.Promise;
  }
  function IsDate2(value) {
    return value instanceof Date && globalThis.Number.isFinite(value.getTime());
  }
  function IsMap(value) {
    return value instanceof globalThis.Map;
  }
  function IsSet(value) {
    return value instanceof globalThis.Set;
  }
  function IsTypedArray(value) {
    return globalThis.ArrayBuffer.isView(value);
  }
  function IsUint8Array2(value) {
    return value instanceof globalThis.Uint8Array;
  }
  function HasPropertyKey2(value, key) {
    return key in value;
  }
  function IsObject2(value) {
    return value !== null && typeof value === "object";
  }
  function IsArray2(value) {
    return globalThis.Array.isArray(value) && !globalThis.ArrayBuffer.isView(value);
  }
  function IsUndefined2(value) {
    return value === void 0;
  }
  function IsNull2(value) {
    return value === null;
  }
  function IsBoolean2(value) {
    return typeof value === "boolean";
  }
  function IsNumber2(value) {
    return typeof value === "number";
  }
  function IsInteger(value) {
    return globalThis.Number.isInteger(value);
  }
  function IsBigInt2(value) {
    return typeof value === "bigint";
  }
  function IsString2(value) {
    return typeof value === "string";
  }
  function IsFunction2(value) {
    return typeof value === "function";
  }
  function IsSymbol2(value) {
    return typeof value === "symbol";
  }
  function IsValueType(value) {
    return IsBigInt2(value) || IsBoolean2(value) || IsNull2(value) || IsNumber2(value) || IsString2(value) || IsSymbol2(value) || IsUndefined2(value);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/system/policy.mjs
  var TypeSystemPolicy;
  (function(TypeSystemPolicy2) {
    TypeSystemPolicy2.InstanceMode = "default";
    TypeSystemPolicy2.ExactOptionalPropertyTypes = false;
    TypeSystemPolicy2.AllowArrayObject = false;
    TypeSystemPolicy2.AllowNaN = false;
    TypeSystemPolicy2.AllowNullVoid = false;
    function IsExactOptionalProperty(value, key) {
      return TypeSystemPolicy2.ExactOptionalPropertyTypes ? key in value : value[key] !== void 0;
    }
    TypeSystemPolicy2.IsExactOptionalProperty = IsExactOptionalProperty;
    function IsObjectLike(value) {
      const isObject = IsObject2(value);
      return TypeSystemPolicy2.AllowArrayObject ? isObject : isObject && !IsArray2(value);
    }
    TypeSystemPolicy2.IsObjectLike = IsObjectLike;
    function IsRecordLike(value) {
      return IsObjectLike(value) && !(value instanceof Date) && !(value instanceof Uint8Array);
    }
    TypeSystemPolicy2.IsRecordLike = IsRecordLike;
    function IsNumberLike(value) {
      return TypeSystemPolicy2.AllowNaN ? IsNumber2(value) : Number.isFinite(value);
    }
    TypeSystemPolicy2.IsNumberLike = IsNumberLike;
    function IsVoidLike(value) {
      const isUndefined = IsUndefined2(value);
      return TypeSystemPolicy2.AllowNullVoid ? isUndefined || value === null : isUndefined;
    }
    TypeSystemPolicy2.IsVoidLike = IsVoidLike;
  })(TypeSystemPolicy || (TypeSystemPolicy = {}));

  // ../../node_modules/@sinclair/typebox/build/esm/type/create/immutable.mjs
  function ImmutableArray(value) {
    return globalThis.Object.freeze(value).map((value2) => Immutable(value2));
  }
  function ImmutableDate(value) {
    return value;
  }
  function ImmutableUint8Array(value) {
    return value;
  }
  function ImmutableRegExp(value) {
    return value;
  }
  function ImmutableObject(value) {
    const result = {};
    for (const key of Object.getOwnPropertyNames(value)) {
      result[key] = Immutable(value[key]);
    }
    for (const key of Object.getOwnPropertySymbols(value)) {
      result[key] = Immutable(value[key]);
    }
    return globalThis.Object.freeze(result);
  }
  function Immutable(value) {
    return IsArray(value) ? ImmutableArray(value) : IsDate(value) ? ImmutableDate(value) : IsUint8Array(value) ? ImmutableUint8Array(value) : IsRegExp(value) ? ImmutableRegExp(value) : IsObject(value) ? ImmutableObject(value) : value;
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/create/type.mjs
  function CreateType(schema, options) {
    const result = options !== void 0 ? { ...options, ...schema } : schema;
    switch (TypeSystemPolicy.InstanceMode) {
      case "freeze":
        return Immutable(result);
      case "clone":
        return Clone(result);
      default:
        return result;
    }
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/error/error.mjs
  var TypeBoxError = class extends Error {
    constructor(message) {
      super(message);
    }
  };

  // ../../node_modules/@sinclair/typebox/build/esm/type/symbols/symbols.mjs
  var TransformKind = Symbol.for("TypeBox.Transform");
  var ReadonlyKind = Symbol.for("TypeBox.Readonly");
  var OptionalKind = Symbol.for("TypeBox.Optional");
  var Hint = Symbol.for("TypeBox.Hint");
  var Kind = Symbol.for("TypeBox.Kind");

  // ../../node_modules/@sinclair/typebox/build/esm/type/guard/kind.mjs
  function IsReadonly(value) {
    return IsObject(value) && value[ReadonlyKind] === "Readonly";
  }
  function IsOptional(value) {
    return IsObject(value) && value[OptionalKind] === "Optional";
  }
  function IsAny(value) {
    return IsKindOf(value, "Any");
  }
  function IsArgument(value) {
    return IsKindOf(value, "Argument");
  }
  function IsArray3(value) {
    return IsKindOf(value, "Array");
  }
  function IsAsyncIterator3(value) {
    return IsKindOf(value, "AsyncIterator");
  }
  function IsBigInt3(value) {
    return IsKindOf(value, "BigInt");
  }
  function IsBoolean3(value) {
    return IsKindOf(value, "Boolean");
  }
  function IsComputed(value) {
    return IsKindOf(value, "Computed");
  }
  function IsConstructor(value) {
    return IsKindOf(value, "Constructor");
  }
  function IsDate3(value) {
    return IsKindOf(value, "Date");
  }
  function IsFunction3(value) {
    return IsKindOf(value, "Function");
  }
  function IsInteger2(value) {
    return IsKindOf(value, "Integer");
  }
  function IsIntersect(value) {
    return IsKindOf(value, "Intersect");
  }
  function IsIterator3(value) {
    return IsKindOf(value, "Iterator");
  }
  function IsKindOf(value, kind2) {
    return IsObject(value) && Kind in value && value[Kind] === kind2;
  }
  function IsLiteralValue(value) {
    return IsBoolean(value) || IsNumber(value) || IsString(value);
  }
  function IsLiteral(value) {
    return IsKindOf(value, "Literal");
  }
  function IsMappedKey(value) {
    return IsKindOf(value, "MappedKey");
  }
  function IsMappedResult(value) {
    return IsKindOf(value, "MappedResult");
  }
  function IsNever(value) {
    return IsKindOf(value, "Never");
  }
  function IsNot(value) {
    return IsKindOf(value, "Not");
  }
  function IsNull3(value) {
    return IsKindOf(value, "Null");
  }
  function IsNumber3(value) {
    return IsKindOf(value, "Number");
  }
  function IsObject3(value) {
    return IsKindOf(value, "Object");
  }
  function IsPromise2(value) {
    return IsKindOf(value, "Promise");
  }
  function IsRecord(value) {
    return IsKindOf(value, "Record");
  }
  function IsRef(value) {
    return IsKindOf(value, "Ref");
  }
  function IsRegExp2(value) {
    return IsKindOf(value, "RegExp");
  }
  function IsString3(value) {
    return IsKindOf(value, "String");
  }
  function IsSymbol3(value) {
    return IsKindOf(value, "Symbol");
  }
  function IsTemplateLiteral(value) {
    return IsKindOf(value, "TemplateLiteral");
  }
  function IsThis(value) {
    return IsKindOf(value, "This");
  }
  function IsTransform(value) {
    return IsObject(value) && TransformKind in value;
  }
  function IsTuple(value) {
    return IsKindOf(value, "Tuple");
  }
  function IsUndefined3(value) {
    return IsKindOf(value, "Undefined");
  }
  function IsUnion(value) {
    return IsKindOf(value, "Union");
  }
  function IsUint8Array3(value) {
    return IsKindOf(value, "Uint8Array");
  }
  function IsUnknown(value) {
    return IsKindOf(value, "Unknown");
  }
  function IsUnsafe(value) {
    return IsKindOf(value, "Unsafe");
  }
  function IsVoid(value) {
    return IsKindOf(value, "Void");
  }
  function IsKind(value) {
    return IsObject(value) && Kind in value && IsString(value[Kind]);
  }
  function IsSchema(value) {
    return IsAny(value) || IsArgument(value) || IsArray3(value) || IsBoolean3(value) || IsBigInt3(value) || IsAsyncIterator3(value) || IsComputed(value) || IsConstructor(value) || IsDate3(value) || IsFunction3(value) || IsInteger2(value) || IsIntersect(value) || IsIterator3(value) || IsLiteral(value) || IsMappedKey(value) || IsMappedResult(value) || IsNever(value) || IsNot(value) || IsNull3(value) || IsNumber3(value) || IsObject3(value) || IsPromise2(value) || IsRecord(value) || IsRef(value) || IsRegExp2(value) || IsString3(value) || IsSymbol3(value) || IsTemplateLiteral(value) || IsThis(value) || IsTuple(value) || IsUndefined3(value) || IsUnion(value) || IsUint8Array3(value) || IsUnknown(value) || IsUnsafe(value) || IsVoid(value) || IsKind(value);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/guard/type.mjs
  var type_exports = {};
  __export(type_exports, {
    IsAny: () => IsAny2,
    IsArgument: () => IsArgument2,
    IsArray: () => IsArray4,
    IsAsyncIterator: () => IsAsyncIterator4,
    IsBigInt: () => IsBigInt4,
    IsBoolean: () => IsBoolean4,
    IsComputed: () => IsComputed2,
    IsConstructor: () => IsConstructor2,
    IsDate: () => IsDate4,
    IsFunction: () => IsFunction4,
    IsImport: () => IsImport,
    IsInteger: () => IsInteger3,
    IsIntersect: () => IsIntersect2,
    IsIterator: () => IsIterator4,
    IsKind: () => IsKind2,
    IsKindOf: () => IsKindOf2,
    IsLiteral: () => IsLiteral2,
    IsLiteralBoolean: () => IsLiteralBoolean,
    IsLiteralNumber: () => IsLiteralNumber,
    IsLiteralString: () => IsLiteralString,
    IsLiteralValue: () => IsLiteralValue2,
    IsMappedKey: () => IsMappedKey2,
    IsMappedResult: () => IsMappedResult2,
    IsNever: () => IsNever2,
    IsNot: () => IsNot2,
    IsNull: () => IsNull4,
    IsNumber: () => IsNumber4,
    IsObject: () => IsObject4,
    IsOptional: () => IsOptional2,
    IsPromise: () => IsPromise3,
    IsProperties: () => IsProperties,
    IsReadonly: () => IsReadonly2,
    IsRecord: () => IsRecord2,
    IsRecursive: () => IsRecursive,
    IsRef: () => IsRef2,
    IsRegExp: () => IsRegExp3,
    IsSchema: () => IsSchema2,
    IsString: () => IsString4,
    IsSymbol: () => IsSymbol4,
    IsTemplateLiteral: () => IsTemplateLiteral2,
    IsThis: () => IsThis2,
    IsTransform: () => IsTransform2,
    IsTuple: () => IsTuple2,
    IsUint8Array: () => IsUint8Array4,
    IsUndefined: () => IsUndefined4,
    IsUnion: () => IsUnion2,
    IsUnionLiteral: () => IsUnionLiteral,
    IsUnknown: () => IsUnknown2,
    IsUnsafe: () => IsUnsafe2,
    IsVoid: () => IsVoid2,
    TypeGuardUnknownTypeError: () => TypeGuardUnknownTypeError
  });
  var TypeGuardUnknownTypeError = class extends TypeBoxError {
  };
  var KnownTypes = [
    "Argument",
    "Any",
    "Array",
    "AsyncIterator",
    "BigInt",
    "Boolean",
    "Computed",
    "Constructor",
    "Date",
    "Enum",
    "Function",
    "Integer",
    "Intersect",
    "Iterator",
    "Literal",
    "MappedKey",
    "MappedResult",
    "Not",
    "Null",
    "Number",
    "Object",
    "Promise",
    "Record",
    "Ref",
    "RegExp",
    "String",
    "Symbol",
    "TemplateLiteral",
    "This",
    "Tuple",
    "Undefined",
    "Union",
    "Uint8Array",
    "Unknown",
    "Void"
  ];
  function IsPattern(value) {
    try {
      new RegExp(value);
      return true;
    } catch {
      return false;
    }
  }
  function IsControlCharacterFree(value) {
    if (!IsString(value))
      return false;
    for (let i = 0; i < value.length; i++) {
      const code = value.charCodeAt(i);
      if (code >= 7 && code <= 13 || code === 27 || code === 127) {
        return false;
      }
    }
    return true;
  }
  function IsAdditionalProperties(value) {
    return IsOptionalBoolean(value) || IsSchema2(value);
  }
  function IsOptionalBigInt(value) {
    return IsUndefined(value) || IsBigInt(value);
  }
  function IsOptionalNumber(value) {
    return IsUndefined(value) || IsNumber(value);
  }
  function IsOptionalBoolean(value) {
    return IsUndefined(value) || IsBoolean(value);
  }
  function IsOptionalString(value) {
    return IsUndefined(value) || IsString(value);
  }
  function IsOptionalPattern(value) {
    return IsUndefined(value) || IsString(value) && IsControlCharacterFree(value) && IsPattern(value);
  }
  function IsOptionalFormat(value) {
    return IsUndefined(value) || IsString(value) && IsControlCharacterFree(value);
  }
  function IsOptionalSchema(value) {
    return IsUndefined(value) || IsSchema2(value);
  }
  function IsReadonly2(value) {
    return IsObject(value) && value[ReadonlyKind] === "Readonly";
  }
  function IsOptional2(value) {
    return IsObject(value) && value[OptionalKind] === "Optional";
  }
  function IsAny2(value) {
    return IsKindOf2(value, "Any") && IsOptionalString(value.$id);
  }
  function IsArgument2(value) {
    return IsKindOf2(value, "Argument") && IsNumber(value.index);
  }
  function IsArray4(value) {
    return IsKindOf2(value, "Array") && value.type === "array" && IsOptionalString(value.$id) && IsSchema2(value.items) && IsOptionalNumber(value.minItems) && IsOptionalNumber(value.maxItems) && IsOptionalBoolean(value.uniqueItems) && IsOptionalSchema(value.contains) && IsOptionalNumber(value.minContains) && IsOptionalNumber(value.maxContains);
  }
  function IsAsyncIterator4(value) {
    return IsKindOf2(value, "AsyncIterator") && value.type === "AsyncIterator" && IsOptionalString(value.$id) && IsSchema2(value.items);
  }
  function IsBigInt4(value) {
    return IsKindOf2(value, "BigInt") && value.type === "bigint" && IsOptionalString(value.$id) && IsOptionalBigInt(value.exclusiveMaximum) && IsOptionalBigInt(value.exclusiveMinimum) && IsOptionalBigInt(value.maximum) && IsOptionalBigInt(value.minimum) && IsOptionalBigInt(value.multipleOf);
  }
  function IsBoolean4(value) {
    return IsKindOf2(value, "Boolean") && value.type === "boolean" && IsOptionalString(value.$id);
  }
  function IsComputed2(value) {
    return IsKindOf2(value, "Computed") && IsString(value.target) && IsArray(value.parameters) && value.parameters.every((schema) => IsSchema2(schema));
  }
  function IsConstructor2(value) {
    return IsKindOf2(value, "Constructor") && value.type === "Constructor" && IsOptionalString(value.$id) && IsArray(value.parameters) && value.parameters.every((schema) => IsSchema2(schema)) && IsSchema2(value.returns);
  }
  function IsDate4(value) {
    return IsKindOf2(value, "Date") && value.type === "Date" && IsOptionalString(value.$id) && IsOptionalNumber(value.exclusiveMaximumTimestamp) && IsOptionalNumber(value.exclusiveMinimumTimestamp) && IsOptionalNumber(value.maximumTimestamp) && IsOptionalNumber(value.minimumTimestamp) && IsOptionalNumber(value.multipleOfTimestamp);
  }
  function IsFunction4(value) {
    return IsKindOf2(value, "Function") && value.type === "Function" && IsOptionalString(value.$id) && IsArray(value.parameters) && value.parameters.every((schema) => IsSchema2(schema)) && IsSchema2(value.returns);
  }
  function IsImport(value) {
    return IsKindOf2(value, "Import") && HasPropertyKey(value, "$defs") && IsObject(value.$defs) && IsProperties(value.$defs) && HasPropertyKey(value, "$ref") && IsString(value.$ref) && value.$ref in value.$defs;
  }
  function IsInteger3(value) {
    return IsKindOf2(value, "Integer") && value.type === "integer" && IsOptionalString(value.$id) && IsOptionalNumber(value.exclusiveMaximum) && IsOptionalNumber(value.exclusiveMinimum) && IsOptionalNumber(value.maximum) && IsOptionalNumber(value.minimum) && IsOptionalNumber(value.multipleOf);
  }
  function IsProperties(value) {
    return IsObject(value) && Object.entries(value).every(([key, schema]) => IsControlCharacterFree(key) && IsSchema2(schema));
  }
  function IsIntersect2(value) {
    return IsKindOf2(value, "Intersect") && (IsString(value.type) && value.type !== "object" ? false : true) && IsArray(value.allOf) && value.allOf.every((schema) => IsSchema2(schema) && !IsTransform2(schema)) && IsOptionalString(value.type) && (IsOptionalBoolean(value.unevaluatedProperties) || IsOptionalSchema(value.unevaluatedProperties)) && IsOptionalString(value.$id);
  }
  function IsIterator4(value) {
    return IsKindOf2(value, "Iterator") && value.type === "Iterator" && IsOptionalString(value.$id) && IsSchema2(value.items);
  }
  function IsKindOf2(value, kind2) {
    return IsObject(value) && Kind in value && value[Kind] === kind2;
  }
  function IsLiteralString(value) {
    return IsLiteral2(value) && IsString(value.const);
  }
  function IsLiteralNumber(value) {
    return IsLiteral2(value) && IsNumber(value.const);
  }
  function IsLiteralBoolean(value) {
    return IsLiteral2(value) && IsBoolean(value.const);
  }
  function IsLiteral2(value) {
    return IsKindOf2(value, "Literal") && IsOptionalString(value.$id) && IsLiteralValue2(value.const);
  }
  function IsLiteralValue2(value) {
    return IsBoolean(value) || IsNumber(value) || IsString(value);
  }
  function IsMappedKey2(value) {
    return IsKindOf2(value, "MappedKey") && IsArray(value.keys) && value.keys.every((key) => IsNumber(key) || IsString(key));
  }
  function IsMappedResult2(value) {
    return IsKindOf2(value, "MappedResult") && IsProperties(value.properties);
  }
  function IsNever2(value) {
    return IsKindOf2(value, "Never") && IsObject(value.not) && Object.getOwnPropertyNames(value.not).length === 0;
  }
  function IsNot2(value) {
    return IsKindOf2(value, "Not") && IsSchema2(value.not);
  }
  function IsNull4(value) {
    return IsKindOf2(value, "Null") && value.type === "null" && IsOptionalString(value.$id);
  }
  function IsNumber4(value) {
    return IsKindOf2(value, "Number") && value.type === "number" && IsOptionalString(value.$id) && IsOptionalNumber(value.exclusiveMaximum) && IsOptionalNumber(value.exclusiveMinimum) && IsOptionalNumber(value.maximum) && IsOptionalNumber(value.minimum) && IsOptionalNumber(value.multipleOf);
  }
  function IsObject4(value) {
    return IsKindOf2(value, "Object") && value.type === "object" && IsOptionalString(value.$id) && IsProperties(value.properties) && IsAdditionalProperties(value.additionalProperties) && IsOptionalNumber(value.minProperties) && IsOptionalNumber(value.maxProperties);
  }
  function IsPromise3(value) {
    return IsKindOf2(value, "Promise") && value.type === "Promise" && IsOptionalString(value.$id) && IsSchema2(value.item);
  }
  function IsRecord2(value) {
    return IsKindOf2(value, "Record") && value.type === "object" && IsOptionalString(value.$id) && IsAdditionalProperties(value.additionalProperties) && IsObject(value.patternProperties) && ((schema) => {
      const keys = Object.getOwnPropertyNames(schema.patternProperties);
      return keys.length === 1 && IsPattern(keys[0]) && IsObject(schema.patternProperties) && IsSchema2(schema.patternProperties[keys[0]]);
    })(value);
  }
  function IsRecursive(value) {
    return IsObject(value) && Hint in value && value[Hint] === "Recursive";
  }
  function IsRef2(value) {
    return IsKindOf2(value, "Ref") && IsOptionalString(value.$id) && IsString(value.$ref);
  }
  function IsRegExp3(value) {
    return IsKindOf2(value, "RegExp") && IsOptionalString(value.$id) && IsString(value.source) && IsString(value.flags) && IsOptionalNumber(value.maxLength) && IsOptionalNumber(value.minLength);
  }
  function IsString4(value) {
    return IsKindOf2(value, "String") && value.type === "string" && IsOptionalString(value.$id) && IsOptionalNumber(value.minLength) && IsOptionalNumber(value.maxLength) && IsOptionalPattern(value.pattern) && IsOptionalFormat(value.format);
  }
  function IsSymbol4(value) {
    return IsKindOf2(value, "Symbol") && value.type === "symbol" && IsOptionalString(value.$id);
  }
  function IsTemplateLiteral2(value) {
    return IsKindOf2(value, "TemplateLiteral") && value.type === "string" && IsString(value.pattern) && value.pattern[0] === "^" && value.pattern[value.pattern.length - 1] === "$";
  }
  function IsThis2(value) {
    return IsKindOf2(value, "This") && IsOptionalString(value.$id) && IsString(value.$ref);
  }
  function IsTransform2(value) {
    return IsObject(value) && TransformKind in value;
  }
  function IsTuple2(value) {
    return IsKindOf2(value, "Tuple") && value.type === "array" && IsOptionalString(value.$id) && IsNumber(value.minItems) && IsNumber(value.maxItems) && value.minItems === value.maxItems && // empty
    (IsUndefined(value.items) && IsUndefined(value.additionalItems) && value.minItems === 0 || IsArray(value.items) && value.items.every((schema) => IsSchema2(schema)));
  }
  function IsUndefined4(value) {
    return IsKindOf2(value, "Undefined") && value.type === "undefined" && IsOptionalString(value.$id);
  }
  function IsUnionLiteral(value) {
    return IsUnion2(value) && value.anyOf.every((schema) => IsLiteralString(schema) || IsLiteralNumber(schema));
  }
  function IsUnion2(value) {
    return IsKindOf2(value, "Union") && IsOptionalString(value.$id) && IsObject(value) && IsArray(value.anyOf) && value.anyOf.every((schema) => IsSchema2(schema));
  }
  function IsUint8Array4(value) {
    return IsKindOf2(value, "Uint8Array") && value.type === "Uint8Array" && IsOptionalString(value.$id) && IsOptionalNumber(value.minByteLength) && IsOptionalNumber(value.maxByteLength);
  }
  function IsUnknown2(value) {
    return IsKindOf2(value, "Unknown") && IsOptionalString(value.$id);
  }
  function IsUnsafe2(value) {
    return IsKindOf2(value, "Unsafe");
  }
  function IsVoid2(value) {
    return IsKindOf2(value, "Void") && value.type === "void" && IsOptionalString(value.$id);
  }
  function IsKind2(value) {
    return IsObject(value) && Kind in value && IsString(value[Kind]) && !KnownTypes.includes(value[Kind]);
  }
  function IsSchema2(value) {
    return IsObject(value) && (IsAny2(value) || IsArgument2(value) || IsArray4(value) || IsBoolean4(value) || IsBigInt4(value) || IsAsyncIterator4(value) || IsComputed2(value) || IsConstructor2(value) || IsDate4(value) || IsFunction4(value) || IsInteger3(value) || IsIntersect2(value) || IsIterator4(value) || IsLiteral2(value) || IsMappedKey2(value) || IsMappedResult2(value) || IsNever2(value) || IsNot2(value) || IsNull4(value) || IsNumber4(value) || IsObject4(value) || IsPromise3(value) || IsRecord2(value) || IsRef2(value) || IsRegExp3(value) || IsString4(value) || IsSymbol4(value) || IsTemplateLiteral2(value) || IsThis2(value) || IsTuple2(value) || IsUndefined4(value) || IsUnion2(value) || IsUint8Array4(value) || IsUnknown2(value) || IsUnsafe2(value) || IsVoid2(value) || IsKind2(value));
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/patterns/patterns.mjs
  var PatternBoolean = "(true|false)";
  var PatternNumber = "(0|[1-9][0-9]*)";
  var PatternString = "(.*)";
  var PatternNever = "(?!.*)";
  var PatternBooleanExact = `^${PatternBoolean}$`;
  var PatternNumberExact = `^${PatternNumber}$`;
  var PatternStringExact = `^${PatternString}$`;
  var PatternNeverExact = `^${PatternNever}$`;

  // ../../node_modules/@sinclair/typebox/build/esm/type/registry/format.mjs
  var format_exports = {};
  __export(format_exports, {
    Clear: () => Clear,
    Delete: () => Delete,
    Entries: () => Entries,
    Get: () => Get,
    Has: () => Has,
    Set: () => Set2
  });
  var map = /* @__PURE__ */ new Map();
  function Entries() {
    return new Map(map);
  }
  function Clear() {
    return map.clear();
  }
  function Delete(format) {
    return map.delete(format);
  }
  function Has(format) {
    return map.has(format);
  }
  function Set2(format, func) {
    map.set(format, func);
  }
  function Get(format) {
    return map.get(format);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/registry/type.mjs
  var type_exports2 = {};
  __export(type_exports2, {
    Clear: () => Clear2,
    Delete: () => Delete2,
    Entries: () => Entries2,
    Get: () => Get2,
    Has: () => Has2,
    Set: () => Set3
  });
  var map2 = /* @__PURE__ */ new Map();
  function Entries2() {
    return new Map(map2);
  }
  function Clear2() {
    return map2.clear();
  }
  function Delete2(kind2) {
    return map2.delete(kind2);
  }
  function Has2(kind2) {
    return map2.has(kind2);
  }
  function Set3(kind2, func) {
    map2.set(kind2, func);
  }
  function Get2(kind2) {
    return map2.get(kind2);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/sets/set.mjs
  function SetIncludes(T, S) {
    return T.includes(S);
  }
  function SetDistinct(T) {
    return [...new Set(T)];
  }
  function SetIntersect(T, S) {
    return T.filter((L) => S.includes(L));
  }
  function SetIntersectManyResolve(T, Init) {
    return T.reduce((Acc, L) => {
      return SetIntersect(Acc, L);
    }, Init);
  }
  function SetIntersectMany(T) {
    return T.length === 1 ? T[0] : T.length > 1 ? SetIntersectManyResolve(T.slice(1), T[0]) : [];
  }
  function SetUnionMany(T) {
    const Acc = [];
    for (const L of T)
      Acc.push(...L);
    return Acc;
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/any/any.mjs
  function Any(options) {
    return CreateType({ [Kind]: "Any" }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/array/array.mjs
  function Array2(items, options) {
    return CreateType({ [Kind]: "Array", type: "array", items }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/argument/argument.mjs
  function Argument(index) {
    return CreateType({ [Kind]: "Argument", index });
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/async-iterator/async-iterator.mjs
  function AsyncIterator(items, options) {
    return CreateType({ [Kind]: "AsyncIterator", type: "AsyncIterator", items }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/computed/computed.mjs
  function Computed(target, parameters, options) {
    return CreateType({ [Kind]: "Computed", target, parameters }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/discard/discard.mjs
  function DiscardKey(value, key) {
    const { [key]: _, ...rest } = value;
    return rest;
  }
  function Discard(value, keys) {
    return keys.reduce((acc, key) => DiscardKey(acc, key), value);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/never/never.mjs
  function Never(options) {
    return CreateType({ [Kind]: "Never", not: {} }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/mapped/mapped-result.mjs
  function MappedResult(properties) {
    return CreateType({
      [Kind]: "MappedResult",
      properties
    });
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/constructor/constructor.mjs
  function Constructor(parameters, returns, options) {
    return CreateType({ [Kind]: "Constructor", type: "Constructor", parameters, returns }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/function/function.mjs
  function Function2(parameters, returns, options) {
    return CreateType({ [Kind]: "Function", type: "Function", parameters, returns }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/union/union-create.mjs
  function UnionCreate(T, options) {
    return CreateType({ [Kind]: "Union", anyOf: T }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/union/union-evaluated.mjs
  function IsUnionOptional(types) {
    return types.some((type) => IsOptional(type));
  }
  function RemoveOptionalFromRest(types) {
    return types.map((left) => IsOptional(left) ? RemoveOptionalFromType(left) : left);
  }
  function RemoveOptionalFromType(T) {
    return Discard(T, [OptionalKind]);
  }
  function ResolveUnion(types, options) {
    const isOptional = IsUnionOptional(types);
    return isOptional ? Optional(UnionCreate(RemoveOptionalFromRest(types), options)) : UnionCreate(RemoveOptionalFromRest(types), options);
  }
  function UnionEvaluated(T, options) {
    return T.length === 1 ? CreateType(T[0], options) : T.length === 0 ? Never(options) : ResolveUnion(T, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/union/union.mjs
  function Union(types, options) {
    return types.length === 0 ? Never(options) : types.length === 1 ? CreateType(types[0], options) : UnionCreate(types, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/template-literal/parse.mjs
  var TemplateLiteralParserError = class extends TypeBoxError {
  };
  function Unescape(pattern) {
    return pattern.replace(/\\\$/g, "$").replace(/\\\*/g, "*").replace(/\\\^/g, "^").replace(/\\\|/g, "|").replace(/\\\(/g, "(").replace(/\\\)/g, ")");
  }
  function IsNonEscaped(pattern, index, char) {
    return pattern[index] === char && pattern.charCodeAt(index - 1) !== 92;
  }
  function IsOpenParen(pattern, index) {
    return IsNonEscaped(pattern, index, "(");
  }
  function IsCloseParen(pattern, index) {
    return IsNonEscaped(pattern, index, ")");
  }
  function IsSeparator(pattern, index) {
    return IsNonEscaped(pattern, index, "|");
  }
  function IsGroup(pattern) {
    if (!(IsOpenParen(pattern, 0) && IsCloseParen(pattern, pattern.length - 1)))
      return false;
    let count = 0;
    for (let index = 0; index < pattern.length; index++) {
      if (IsOpenParen(pattern, index))
        count += 1;
      if (IsCloseParen(pattern, index))
        count -= 1;
      if (count === 0 && index !== pattern.length - 1)
        return false;
    }
    return true;
  }
  function InGroup(pattern) {
    return pattern.slice(1, pattern.length - 1);
  }
  function IsPrecedenceOr(pattern) {
    let count = 0;
    for (let index = 0; index < pattern.length; index++) {
      if (IsOpenParen(pattern, index))
        count += 1;
      if (IsCloseParen(pattern, index))
        count -= 1;
      if (IsSeparator(pattern, index) && count === 0)
        return true;
    }
    return false;
  }
  function IsPrecedenceAnd(pattern) {
    for (let index = 0; index < pattern.length; index++) {
      if (IsOpenParen(pattern, index))
        return true;
    }
    return false;
  }
  function Or(pattern) {
    let [count, start] = [0, 0];
    const expressions = [];
    for (let index = 0; index < pattern.length; index++) {
      if (IsOpenParen(pattern, index))
        count += 1;
      if (IsCloseParen(pattern, index))
        count -= 1;
      if (IsSeparator(pattern, index) && count === 0) {
        const range2 = pattern.slice(start, index);
        if (range2.length > 0)
          expressions.push(TemplateLiteralParse(range2));
        start = index + 1;
      }
    }
    const range = pattern.slice(start);
    if (range.length > 0)
      expressions.push(TemplateLiteralParse(range));
    if (expressions.length === 0)
      return { type: "const", const: "" };
    if (expressions.length === 1)
      return expressions[0];
    return { type: "or", expr: expressions };
  }
  function And(pattern) {
    function Group(value, index) {
      if (!IsOpenParen(value, index))
        throw new TemplateLiteralParserError(`TemplateLiteralParser: Index must point to open parens`);
      let count = 0;
      for (let scan = index; scan < value.length; scan++) {
        if (IsOpenParen(value, scan))
          count += 1;
        if (IsCloseParen(value, scan))
          count -= 1;
        if (count === 0)
          return [index, scan];
      }
      throw new TemplateLiteralParserError(`TemplateLiteralParser: Unclosed group parens in expression`);
    }
    function Range(pattern2, index) {
      for (let scan = index; scan < pattern2.length; scan++) {
        if (IsOpenParen(pattern2, scan))
          return [index, scan];
      }
      return [index, pattern2.length];
    }
    const expressions = [];
    for (let index = 0; index < pattern.length; index++) {
      if (IsOpenParen(pattern, index)) {
        const [start, end] = Group(pattern, index);
        const range = pattern.slice(start, end + 1);
        expressions.push(TemplateLiteralParse(range));
        index = end;
      } else {
        const [start, end] = Range(pattern, index);
        const range = pattern.slice(start, end);
        if (range.length > 0)
          expressions.push(TemplateLiteralParse(range));
        index = end - 1;
      }
    }
    return expressions.length === 0 ? { type: "const", const: "" } : expressions.length === 1 ? expressions[0] : { type: "and", expr: expressions };
  }
  function TemplateLiteralParse(pattern) {
    return IsGroup(pattern) ? TemplateLiteralParse(InGroup(pattern)) : IsPrecedenceOr(pattern) ? Or(pattern) : IsPrecedenceAnd(pattern) ? And(pattern) : { type: "const", const: Unescape(pattern) };
  }
  function TemplateLiteralParseExact(pattern) {
    return TemplateLiteralParse(pattern.slice(1, pattern.length - 1));
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/template-literal/finite.mjs
  var TemplateLiteralFiniteError = class extends TypeBoxError {
  };
  function IsNumberExpression(expression) {
    return expression.type === "or" && expression.expr.length === 2 && expression.expr[0].type === "const" && expression.expr[0].const === "0" && expression.expr[1].type === "const" && expression.expr[1].const === "[1-9][0-9]*";
  }
  function IsBooleanExpression(expression) {
    return expression.type === "or" && expression.expr.length === 2 && expression.expr[0].type === "const" && expression.expr[0].const === "true" && expression.expr[1].type === "const" && expression.expr[1].const === "false";
  }
  function IsStringExpression(expression) {
    return expression.type === "const" && expression.const === ".*";
  }
  function IsTemplateLiteralExpressionFinite(expression) {
    return IsNumberExpression(expression) || IsStringExpression(expression) ? false : IsBooleanExpression(expression) ? true : expression.type === "and" ? expression.expr.every((expr) => IsTemplateLiteralExpressionFinite(expr)) : expression.type === "or" ? expression.expr.every((expr) => IsTemplateLiteralExpressionFinite(expr)) : expression.type === "const" ? true : (() => {
      throw new TemplateLiteralFiniteError(`Unknown expression type`);
    })();
  }
  function IsTemplateLiteralFinite(schema) {
    const expression = TemplateLiteralParseExact(schema.pattern);
    return IsTemplateLiteralExpressionFinite(expression);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/template-literal/generate.mjs
  var TemplateLiteralGenerateError = class extends TypeBoxError {
  };
  function* GenerateReduce(buffer) {
    if (buffer.length === 1)
      return yield* buffer[0];
    for (const left of buffer[0]) {
      for (const right of GenerateReduce(buffer.slice(1))) {
        yield `${left}${right}`;
      }
    }
  }
  function* GenerateAnd(expression) {
    return yield* GenerateReduce(expression.expr.map((expr) => [...TemplateLiteralExpressionGenerate(expr)]));
  }
  function* GenerateOr(expression) {
    for (const expr of expression.expr)
      yield* TemplateLiteralExpressionGenerate(expr);
  }
  function* GenerateConst(expression) {
    return yield expression.const;
  }
  function* TemplateLiteralExpressionGenerate(expression) {
    return expression.type === "and" ? yield* GenerateAnd(expression) : expression.type === "or" ? yield* GenerateOr(expression) : expression.type === "const" ? yield* GenerateConst(expression) : (() => {
      throw new TemplateLiteralGenerateError("Unknown expression");
    })();
  }
  function TemplateLiteralGenerate(schema) {
    const expression = TemplateLiteralParseExact(schema.pattern);
    return IsTemplateLiteralExpressionFinite(expression) ? [...TemplateLiteralExpressionGenerate(expression)] : [];
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/literal/literal.mjs
  function Literal(value, options) {
    return CreateType({
      [Kind]: "Literal",
      const: value,
      type: typeof value
    }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/boolean/boolean.mjs
  function Boolean2(options) {
    return CreateType({ [Kind]: "Boolean", type: "boolean" }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/bigint/bigint.mjs
  function BigInt2(options) {
    return CreateType({ [Kind]: "BigInt", type: "bigint" }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/number/number.mjs
  function Number2(options) {
    return CreateType({ [Kind]: "Number", type: "number" }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/string/string.mjs
  function String2(options) {
    return CreateType({ [Kind]: "String", type: "string" }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/template-literal/syntax.mjs
  function* FromUnion(syntax) {
    const trim = syntax.trim().replace(/"|'/g, "");
    return trim === "boolean" ? yield Boolean2() : trim === "number" ? yield Number2() : trim === "bigint" ? yield BigInt2() : trim === "string" ? yield String2() : yield (() => {
      const literals = trim.split("|").map((literal) => Literal(literal.trim()));
      return literals.length === 0 ? Never() : literals.length === 1 ? literals[0] : UnionEvaluated(literals);
    })();
  }
  function* FromTerminal(syntax) {
    if (syntax[1] !== "{") {
      const L = Literal("$");
      const R = FromSyntax(syntax.slice(1));
      return yield* [L, ...R];
    }
    for (let i = 2; i < syntax.length; i++) {
      if (syntax[i] === "}") {
        const L = FromUnion(syntax.slice(2, i));
        const R = FromSyntax(syntax.slice(i + 1));
        return yield* [...L, ...R];
      }
    }
    yield Literal(syntax);
  }
  function* FromSyntax(syntax) {
    for (let i = 0; i < syntax.length; i++) {
      if (syntax[i] === "$") {
        const L = Literal(syntax.slice(0, i));
        const R = FromTerminal(syntax.slice(i));
        return yield* [L, ...R];
      }
    }
    yield Literal(syntax);
  }
  function TemplateLiteralSyntax(syntax) {
    return [...FromSyntax(syntax)];
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/template-literal/pattern.mjs
  var TemplateLiteralPatternError = class extends TypeBoxError {
  };
  function Escape(value) {
    return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
  }
  function Visit2(schema, acc) {
    return IsTemplateLiteral(schema) ? schema.pattern.slice(1, schema.pattern.length - 1) : IsUnion(schema) ? `(${schema.anyOf.map((schema2) => Visit2(schema2, acc)).join("|")})` : IsNumber3(schema) ? `${acc}${PatternNumber}` : IsInteger2(schema) ? `${acc}${PatternNumber}` : IsBigInt3(schema) ? `${acc}${PatternNumber}` : IsString3(schema) ? `${acc}${PatternString}` : IsLiteral(schema) ? `${acc}${Escape(schema.const.toString())}` : IsBoolean3(schema) ? `${acc}${PatternBoolean}` : (() => {
      throw new TemplateLiteralPatternError(`Unexpected Kind '${schema[Kind]}'`);
    })();
  }
  function TemplateLiteralPattern(kinds) {
    return `^${kinds.map((schema) => Visit2(schema, "")).join("")}$`;
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/template-literal/union.mjs
  function TemplateLiteralToUnion(schema) {
    const R = TemplateLiteralGenerate(schema);
    const L = R.map((S) => Literal(S));
    return UnionEvaluated(L);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/template-literal/template-literal.mjs
  function TemplateLiteral(unresolved, options) {
    const pattern = IsString(unresolved) ? TemplateLiteralPattern(TemplateLiteralSyntax(unresolved)) : TemplateLiteralPattern(unresolved);
    return CreateType({ [Kind]: "TemplateLiteral", type: "string", pattern }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/indexed/indexed-property-keys.mjs
  function FromTemplateLiteral(templateLiteral) {
    const keys = TemplateLiteralGenerate(templateLiteral);
    return keys.map((key) => key.toString());
  }
  function FromUnion2(types) {
    const result = [];
    for (const type of types)
      result.push(...IndexPropertyKeys(type));
    return result;
  }
  function FromLiteral(literalValue) {
    return [literalValue.toString()];
  }
  function IndexPropertyKeys(type) {
    return [...new Set(IsTemplateLiteral(type) ? FromTemplateLiteral(type) : IsUnion(type) ? FromUnion2(type.anyOf) : IsLiteral(type) ? FromLiteral(type.const) : IsNumber3(type) ? ["[number]"] : IsInteger2(type) ? ["[number]"] : [])];
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/indexed/indexed-from-mapped-result.mjs
  function FromProperties(type, properties, options) {
    const result = {};
    for (const K2 of Object.getOwnPropertyNames(properties)) {
      result[K2] = Index(type, IndexPropertyKeys(properties[K2]), options);
    }
    return result;
  }
  function FromMappedResult(type, mappedResult, options) {
    return FromProperties(type, mappedResult.properties, options);
  }
  function IndexFromMappedResult(type, mappedResult, options) {
    const properties = FromMappedResult(type, mappedResult, options);
    return MappedResult(properties);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/indexed/indexed.mjs
  function FromRest(types, key) {
    return types.map((type) => IndexFromPropertyKey(type, key));
  }
  function FromIntersectRest(types) {
    return types.filter((type) => !IsNever(type));
  }
  function FromIntersect(types, key) {
    return IntersectEvaluated(FromIntersectRest(FromRest(types, key)));
  }
  function FromUnionRest(types) {
    return types.some((L) => IsNever(L)) ? [] : types;
  }
  function FromUnion3(types, key) {
    return UnionEvaluated(FromUnionRest(FromRest(types, key)));
  }
  function FromTuple(types, key) {
    return key in types ? types[key] : key === "[number]" ? UnionEvaluated(types) : Never();
  }
  function FromArray(type, key) {
    return key === "[number]" ? type : Never();
  }
  function FromProperty(properties, propertyKey) {
    return propertyKey in properties ? properties[propertyKey] : Never();
  }
  function IndexFromPropertyKey(type, propertyKey) {
    return IsIntersect(type) ? FromIntersect(type.allOf, propertyKey) : IsUnion(type) ? FromUnion3(type.anyOf, propertyKey) : IsTuple(type) ? FromTuple(type.items ?? [], propertyKey) : IsArray3(type) ? FromArray(type.items, propertyKey) : IsObject3(type) ? FromProperty(type.properties, propertyKey) : Never();
  }
  function IndexFromPropertyKeys(type, propertyKeys) {
    return propertyKeys.map((propertyKey) => IndexFromPropertyKey(type, propertyKey));
  }
  function FromSchema(type, propertyKeys) {
    return UnionEvaluated(IndexFromPropertyKeys(type, propertyKeys));
  }
  function Index(type, key, options) {
    if (IsRef(type) || IsRef(key)) {
      const error = `Index types using Ref parameters require both Type and Key to be of TSchema`;
      if (!IsSchema(type) || !IsSchema(key))
        throw new TypeBoxError(error);
      return Computed("Index", [type, key]);
    }
    if (IsMappedResult(key))
      return IndexFromMappedResult(type, key, options);
    if (IsMappedKey(key))
      return IndexFromMappedKey(type, key, options);
    return CreateType(IsSchema(key) ? FromSchema(type, IndexPropertyKeys(key)) : FromSchema(type, key), options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/indexed/indexed-from-mapped-key.mjs
  function MappedIndexPropertyKey(type, key, options) {
    return { [key]: Index(type, [key], Clone(options)) };
  }
  function MappedIndexPropertyKeys(type, propertyKeys, options) {
    return propertyKeys.reduce((result, left) => {
      return { ...result, ...MappedIndexPropertyKey(type, left, options) };
    }, {});
  }
  function MappedIndexProperties(type, mappedKey, options) {
    return MappedIndexPropertyKeys(type, mappedKey.keys, options);
  }
  function IndexFromMappedKey(type, mappedKey, options) {
    const properties = MappedIndexProperties(type, mappedKey, options);
    return MappedResult(properties);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/iterator/iterator.mjs
  function Iterator(items, options) {
    return CreateType({ [Kind]: "Iterator", type: "Iterator", items }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/object/object.mjs
  function RequiredArray(properties) {
    return globalThis.Object.keys(properties).filter((key) => !IsOptional(properties[key]));
  }
  function _Object(properties, options) {
    const required = RequiredArray(properties);
    const schema = required.length > 0 ? { [Kind]: "Object", type: "object", required, properties } : { [Kind]: "Object", type: "object", properties };
    return CreateType(schema, options);
  }
  var Object2 = _Object;

  // ../../node_modules/@sinclair/typebox/build/esm/type/promise/promise.mjs
  function Promise2(item, options) {
    return CreateType({ [Kind]: "Promise", type: "Promise", item }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/readonly/readonly.mjs
  function RemoveReadonly(schema) {
    return CreateType(Discard(schema, [ReadonlyKind]));
  }
  function AddReadonly(schema) {
    return CreateType({ ...schema, [ReadonlyKind]: "Readonly" });
  }
  function ReadonlyWithFlag(schema, F) {
    return F === false ? RemoveReadonly(schema) : AddReadonly(schema);
  }
  function Readonly(schema, enable) {
    const F = enable ?? true;
    return IsMappedResult(schema) ? ReadonlyFromMappedResult(schema, F) : ReadonlyWithFlag(schema, F);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/readonly/readonly-from-mapped-result.mjs
  function FromProperties2(K, F) {
    const Acc = {};
    for (const K2 of globalThis.Object.getOwnPropertyNames(K))
      Acc[K2] = Readonly(K[K2], F);
    return Acc;
  }
  function FromMappedResult2(R, F) {
    return FromProperties2(R.properties, F);
  }
  function ReadonlyFromMappedResult(R, F) {
    const P = FromMappedResult2(R, F);
    return MappedResult(P);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/tuple/tuple.mjs
  function Tuple(types, options) {
    return CreateType(types.length > 0 ? { [Kind]: "Tuple", type: "array", items: types, additionalItems: false, minItems: types.length, maxItems: types.length } : { [Kind]: "Tuple", type: "array", minItems: types.length, maxItems: types.length }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/mapped/mapped.mjs
  function FromMappedResult3(K, P) {
    return K in P ? FromSchemaType(K, P[K]) : MappedResult(P);
  }
  function MappedKeyToKnownMappedResultProperties(K) {
    return { [K]: Literal(K) };
  }
  function MappedKeyToUnknownMappedResultProperties(P) {
    const Acc = {};
    for (const L of P)
      Acc[L] = Literal(L);
    return Acc;
  }
  function MappedKeyToMappedResultProperties(K, P) {
    return SetIncludes(P, K) ? MappedKeyToKnownMappedResultProperties(K) : MappedKeyToUnknownMappedResultProperties(P);
  }
  function FromMappedKey(K, P) {
    const R = MappedKeyToMappedResultProperties(K, P);
    return FromMappedResult3(K, R);
  }
  function FromRest2(K, T) {
    return T.map((L) => FromSchemaType(K, L));
  }
  function FromProperties3(K, T) {
    const Acc = {};
    for (const K2 of globalThis.Object.getOwnPropertyNames(T))
      Acc[K2] = FromSchemaType(K, T[K2]);
    return Acc;
  }
  function FromSchemaType(K, T) {
    const options = { ...T };
    return (
      // unevaluated modifier types
      IsOptional(T) ? Optional(FromSchemaType(K, Discard(T, [OptionalKind]))) : IsReadonly(T) ? Readonly(FromSchemaType(K, Discard(T, [ReadonlyKind]))) : (
        // unevaluated mapped types
        IsMappedResult(T) ? FromMappedResult3(K, T.properties) : IsMappedKey(T) ? FromMappedKey(K, T.keys) : (
          // unevaluated types
          IsConstructor(T) ? Constructor(FromRest2(K, T.parameters), FromSchemaType(K, T.returns), options) : IsFunction3(T) ? Function2(FromRest2(K, T.parameters), FromSchemaType(K, T.returns), options) : IsAsyncIterator3(T) ? AsyncIterator(FromSchemaType(K, T.items), options) : IsIterator3(T) ? Iterator(FromSchemaType(K, T.items), options) : IsIntersect(T) ? Intersect(FromRest2(K, T.allOf), options) : IsUnion(T) ? Union(FromRest2(K, T.anyOf), options) : IsTuple(T) ? Tuple(FromRest2(K, T.items ?? []), options) : IsObject3(T) ? Object2(FromProperties3(K, T.properties), options) : IsArray3(T) ? Array2(FromSchemaType(K, T.items), options) : IsPromise2(T) ? Promise2(FromSchemaType(K, T.item), options) : T
        )
      )
    );
  }
  function MappedFunctionReturnType(K, T) {
    const Acc = {};
    for (const L of K)
      Acc[L] = FromSchemaType(L, T);
    return Acc;
  }
  function Mapped(key, map3, options) {
    const K = IsSchema(key) ? IndexPropertyKeys(key) : key;
    const RT = map3({ [Kind]: "MappedKey", keys: K });
    const R = MappedFunctionReturnType(K, RT);
    return Object2(R, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/optional/optional.mjs
  function RemoveOptional(schema) {
    return CreateType(Discard(schema, [OptionalKind]));
  }
  function AddOptional(schema) {
    return CreateType({ ...schema, [OptionalKind]: "Optional" });
  }
  function OptionalWithFlag(schema, F) {
    return F === false ? RemoveOptional(schema) : AddOptional(schema);
  }
  function Optional(schema, enable) {
    const F = enable ?? true;
    return IsMappedResult(schema) ? OptionalFromMappedResult(schema, F) : OptionalWithFlag(schema, F);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/optional/optional-from-mapped-result.mjs
  function FromProperties4(P, F) {
    const Acc = {};
    for (const K2 of globalThis.Object.getOwnPropertyNames(P))
      Acc[K2] = Optional(P[K2], F);
    return Acc;
  }
  function FromMappedResult4(R, F) {
    return FromProperties4(R.properties, F);
  }
  function OptionalFromMappedResult(R, F) {
    const P = FromMappedResult4(R, F);
    return MappedResult(P);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/intersect/intersect-create.mjs
  function IntersectCreate(T, options = {}) {
    const allObjects = T.every((schema) => IsObject3(schema));
    const clonedUnevaluatedProperties = IsSchema(options.unevaluatedProperties) ? { unevaluatedProperties: options.unevaluatedProperties } : {};
    return CreateType(options.unevaluatedProperties === false || IsSchema(options.unevaluatedProperties) || allObjects ? { ...clonedUnevaluatedProperties, [Kind]: "Intersect", type: "object", allOf: T } : { ...clonedUnevaluatedProperties, [Kind]: "Intersect", allOf: T }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/intersect/intersect-evaluated.mjs
  function IsIntersectOptional(types) {
    return types.every((left) => IsOptional(left));
  }
  function RemoveOptionalFromType2(type) {
    return Discard(type, [OptionalKind]);
  }
  function RemoveOptionalFromRest2(types) {
    return types.map((left) => IsOptional(left) ? RemoveOptionalFromType2(left) : left);
  }
  function ResolveIntersect(types, options) {
    return IsIntersectOptional(types) ? Optional(IntersectCreate(RemoveOptionalFromRest2(types), options)) : IntersectCreate(RemoveOptionalFromRest2(types), options);
  }
  function IntersectEvaluated(types, options = {}) {
    if (types.length === 1)
      return CreateType(types[0], options);
    if (types.length === 0)
      return Never(options);
    if (types.some((schema) => IsTransform(schema)))
      throw new Error("Cannot intersect transform types");
    return ResolveIntersect(types, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/intersect/intersect.mjs
  function Intersect(types, options) {
    if (types.length === 1)
      return CreateType(types[0], options);
    if (types.length === 0)
      return Never(options);
    if (types.some((schema) => IsTransform(schema)))
      throw new Error("Cannot intersect transform types");
    return IntersectCreate(types, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/ref/ref.mjs
  function Ref(...args) {
    const [$ref, options] = typeof args[0] === "string" ? [args[0], args[1]] : [args[0].$id, args[1]];
    if (typeof $ref !== "string")
      throw new TypeBoxError("Ref: $ref must be a string");
    return CreateType({ [Kind]: "Ref", $ref }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/awaited/awaited.mjs
  function FromComputed(target, parameters) {
    return Computed("Awaited", [Computed(target, parameters)]);
  }
  function FromRef($ref) {
    return Computed("Awaited", [Ref($ref)]);
  }
  function FromIntersect2(types) {
    return Intersect(FromRest3(types));
  }
  function FromUnion4(types) {
    return Union(FromRest3(types));
  }
  function FromPromise(type) {
    return Awaited(type);
  }
  function FromRest3(types) {
    return types.map((type) => Awaited(type));
  }
  function Awaited(type, options) {
    return CreateType(IsComputed(type) ? FromComputed(type.target, type.parameters) : IsIntersect(type) ? FromIntersect2(type.allOf) : IsUnion(type) ? FromUnion4(type.anyOf) : IsPromise2(type) ? FromPromise(type.item) : IsRef(type) ? FromRef(type.$ref) : type, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/keyof/keyof-property-keys.mjs
  function FromRest4(types) {
    const result = [];
    for (const L of types)
      result.push(KeyOfPropertyKeys(L));
    return result;
  }
  function FromIntersect3(types) {
    const propertyKeysArray = FromRest4(types);
    const propertyKeys = SetUnionMany(propertyKeysArray);
    return propertyKeys;
  }
  function FromUnion5(types) {
    const propertyKeysArray = FromRest4(types);
    const propertyKeys = SetIntersectMany(propertyKeysArray);
    return propertyKeys;
  }
  function FromTuple2(types) {
    return types.map((_, indexer) => indexer.toString());
  }
  function FromArray2(_) {
    return ["[number]"];
  }
  function FromProperties5(T) {
    return globalThis.Object.getOwnPropertyNames(T);
  }
  function FromPatternProperties(patternProperties) {
    if (!includePatternProperties)
      return [];
    const patternPropertyKeys = globalThis.Object.getOwnPropertyNames(patternProperties);
    return patternPropertyKeys.map((key) => {
      return key[0] === "^" && key[key.length - 1] === "$" ? key.slice(1, key.length - 1) : key;
    });
  }
  function KeyOfPropertyKeys(type) {
    return IsIntersect(type) ? FromIntersect3(type.allOf) : IsUnion(type) ? FromUnion5(type.anyOf) : IsTuple(type) ? FromTuple2(type.items ?? []) : IsArray3(type) ? FromArray2(type.items) : IsObject3(type) ? FromProperties5(type.properties) : IsRecord(type) ? FromPatternProperties(type.patternProperties) : [];
  }
  var includePatternProperties = false;
  function KeyOfPattern(schema) {
    includePatternProperties = true;
    const keys = KeyOfPropertyKeys(schema);
    includePatternProperties = false;
    const pattern = keys.map((key) => `(${key})`);
    return `^(${pattern.join("|")})$`;
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/keyof/keyof.mjs
  function FromComputed2(target, parameters) {
    return Computed("KeyOf", [Computed(target, parameters)]);
  }
  function FromRef2($ref) {
    return Computed("KeyOf", [Ref($ref)]);
  }
  function KeyOfFromType(type, options) {
    const propertyKeys = KeyOfPropertyKeys(type);
    const propertyKeyTypes = KeyOfPropertyKeysToRest(propertyKeys);
    const result = UnionEvaluated(propertyKeyTypes);
    return CreateType(result, options);
  }
  function KeyOfPropertyKeysToRest(propertyKeys) {
    return propertyKeys.map((L) => L === "[number]" ? Number2() : Literal(L));
  }
  function KeyOf(type, options) {
    return IsComputed(type) ? FromComputed2(type.target, type.parameters) : IsRef(type) ? FromRef2(type.$ref) : IsMappedResult(type) ? KeyOfFromMappedResult(type, options) : KeyOfFromType(type, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/keyof/keyof-from-mapped-result.mjs
  function FromProperties6(properties, options) {
    const result = {};
    for (const K2 of globalThis.Object.getOwnPropertyNames(properties))
      result[K2] = KeyOf(properties[K2], Clone(options));
    return result;
  }
  function FromMappedResult5(mappedResult, options) {
    return FromProperties6(mappedResult.properties, options);
  }
  function KeyOfFromMappedResult(mappedResult, options) {
    const properties = FromMappedResult5(mappedResult, options);
    return MappedResult(properties);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/keyof/keyof-property-entries.mjs
  function KeyOfPropertyEntries(schema) {
    const keys = KeyOfPropertyKeys(schema);
    const schemas = IndexFromPropertyKeys(schema, keys);
    return keys.map((_, index) => [keys[index], schemas[index]]);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/composite/composite.mjs
  function CompositeKeys(T) {
    const Acc = [];
    for (const L of T)
      Acc.push(...KeyOfPropertyKeys(L));
    return SetDistinct(Acc);
  }
  function FilterNever(T) {
    return T.filter((L) => !IsNever(L));
  }
  function CompositeProperty(T, K) {
    const Acc = [];
    for (const L of T)
      Acc.push(...IndexFromPropertyKeys(L, [K]));
    return FilterNever(Acc);
  }
  function CompositeProperties(T, K) {
    const Acc = {};
    for (const L of K) {
      Acc[L] = IntersectEvaluated(CompositeProperty(T, L));
    }
    return Acc;
  }
  function Composite(T, options) {
    const K = CompositeKeys(T);
    const P = CompositeProperties(T, K);
    const R = Object2(P, options);
    return R;
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/date/date.mjs
  function Date2(options) {
    return CreateType({ [Kind]: "Date", type: "Date" }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/null/null.mjs
  function Null(options) {
    return CreateType({ [Kind]: "Null", type: "null" }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/symbol/symbol.mjs
  function Symbol2(options) {
    return CreateType({ [Kind]: "Symbol", type: "symbol" }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/undefined/undefined.mjs
  function Undefined(options) {
    return CreateType({ [Kind]: "Undefined", type: "undefined" }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/uint8array/uint8array.mjs
  function Uint8Array2(options) {
    return CreateType({ [Kind]: "Uint8Array", type: "Uint8Array" }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/unknown/unknown.mjs
  function Unknown(options) {
    return CreateType({ [Kind]: "Unknown" }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/const/const.mjs
  function FromArray3(T) {
    return T.map((L) => FromValue(L, false));
  }
  function FromProperties7(value) {
    const Acc = {};
    for (const K of globalThis.Object.getOwnPropertyNames(value))
      Acc[K] = Readonly(FromValue(value[K], false));
    return Acc;
  }
  function ConditionalReadonly(T, root) {
    return root === true ? T : Readonly(T);
  }
  function FromValue(value, root) {
    return IsAsyncIterator(value) ? ConditionalReadonly(Any(), root) : IsIterator(value) ? ConditionalReadonly(Any(), root) : IsArray(value) ? Readonly(Tuple(FromArray3(value))) : IsUint8Array(value) ? Uint8Array2() : IsDate(value) ? Date2() : IsObject(value) ? ConditionalReadonly(Object2(FromProperties7(value)), root) : IsFunction(value) ? ConditionalReadonly(Function2([], Unknown()), root) : IsUndefined(value) ? Undefined() : IsNull(value) ? Null() : IsSymbol(value) ? Symbol2() : IsBigInt(value) ? BigInt2() : IsNumber(value) ? Literal(value) : IsBoolean(value) ? Literal(value) : IsString(value) ? Literal(value) : Object2({});
  }
  function Const(T, options) {
    return CreateType(FromValue(T, true), options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/constructor-parameters/constructor-parameters.mjs
  function ConstructorParameters(schema, options) {
    return IsConstructor(schema) ? Tuple(schema.parameters, options) : Never(options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/enum/enum.mjs
  function Enum(item, options) {
    if (IsUndefined(item))
      throw new Error("Enum undefined or empty");
    const values1 = globalThis.Object.getOwnPropertyNames(item).filter((key) => isNaN(key)).map((key) => item[key]);
    const values2 = [...new Set(values1)];
    const anyOf = values2.map((value) => Literal(value));
    return Union(anyOf, { ...options, [Hint]: "Enum" });
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/extends/extends-check.mjs
  var ExtendsResolverError = class extends TypeBoxError {
  };
  var ExtendsResult;
  (function(ExtendsResult2) {
    ExtendsResult2[ExtendsResult2["Union"] = 0] = "Union";
    ExtendsResult2[ExtendsResult2["True"] = 1] = "True";
    ExtendsResult2[ExtendsResult2["False"] = 2] = "False";
  })(ExtendsResult || (ExtendsResult = {}));
  function IntoBooleanResult(result) {
    return result === ExtendsResult.False ? result : ExtendsResult.True;
  }
  function Throw(message) {
    throw new ExtendsResolverError(message);
  }
  function IsStructuralRight(right) {
    return type_exports.IsNever(right) || type_exports.IsIntersect(right) || type_exports.IsUnion(right) || type_exports.IsUnknown(right) || type_exports.IsAny(right);
  }
  function StructuralRight(left, right) {
    return type_exports.IsNever(right) ? FromNeverRight(left, right) : type_exports.IsIntersect(right) ? FromIntersectRight(left, right) : type_exports.IsUnion(right) ? FromUnionRight(left, right) : type_exports.IsUnknown(right) ? FromUnknownRight(left, right) : type_exports.IsAny(right) ? FromAnyRight(left, right) : Throw("StructuralRight");
  }
  function FromAnyRight(left, right) {
    return ExtendsResult.True;
  }
  function FromAny(left, right) {
    return type_exports.IsIntersect(right) ? FromIntersectRight(left, right) : type_exports.IsUnion(right) && right.anyOf.some((schema) => type_exports.IsAny(schema) || type_exports.IsUnknown(schema)) ? ExtendsResult.True : type_exports.IsUnion(right) ? ExtendsResult.Union : type_exports.IsUnknown(right) ? ExtendsResult.True : type_exports.IsAny(right) ? ExtendsResult.True : ExtendsResult.Union;
  }
  function FromArrayRight(left, right) {
    return type_exports.IsUnknown(left) ? ExtendsResult.False : type_exports.IsAny(left) ? ExtendsResult.Union : type_exports.IsNever(left) ? ExtendsResult.True : ExtendsResult.False;
  }
  function FromArray4(left, right) {
    return type_exports.IsObject(right) && IsObjectArrayLike(right) ? ExtendsResult.True : IsStructuralRight(right) ? StructuralRight(left, right) : !type_exports.IsArray(right) ? ExtendsResult.False : IntoBooleanResult(Visit3(left.items, right.items));
  }
  function FromAsyncIterator(left, right) {
    return IsStructuralRight(right) ? StructuralRight(left, right) : !type_exports.IsAsyncIterator(right) ? ExtendsResult.False : IntoBooleanResult(Visit3(left.items, right.items));
  }
  function FromBigInt(left, right) {
    return IsStructuralRight(right) ? StructuralRight(left, right) : type_exports.IsObject(right) ? FromObjectRight(left, right) : type_exports.IsRecord(right) ? FromRecordRight(left, right) : type_exports.IsBigInt(right) ? ExtendsResult.True : ExtendsResult.False;
  }
  function FromBooleanRight(left, right) {
    return type_exports.IsLiteralBoolean(left) ? ExtendsResult.True : type_exports.IsBoolean(left) ? ExtendsResult.True : ExtendsResult.False;
  }
  function FromBoolean(left, right) {
    return IsStructuralRight(right) ? StructuralRight(left, right) : type_exports.IsObject(right) ? FromObjectRight(left, right) : type_exports.IsRecord(right) ? FromRecordRight(left, right) : type_exports.IsBoolean(right) ? ExtendsResult.True : ExtendsResult.False;
  }
  function FromConstructor(left, right) {
    return IsStructuralRight(right) ? StructuralRight(left, right) : type_exports.IsObject(right) ? FromObjectRight(left, right) : !type_exports.IsConstructor(right) ? ExtendsResult.False : left.parameters.length > right.parameters.length ? ExtendsResult.False : !left.parameters.every((schema, index) => IntoBooleanResult(Visit3(right.parameters[index], schema)) === ExtendsResult.True) ? ExtendsResult.False : IntoBooleanResult(Visit3(left.returns, right.returns));
  }
  function FromDate(left, right) {
    return IsStructuralRight(right) ? StructuralRight(left, right) : type_exports.IsObject(right) ? FromObjectRight(left, right) : type_exports.IsRecord(right) ? FromRecordRight(left, right) : type_exports.IsDate(right) ? ExtendsResult.True : ExtendsResult.False;
  }
  function FromFunction(left, right) {
    return IsStructuralRight(right) ? StructuralRight(left, right) : type_exports.IsObject(right) ? FromObjectRight(left, right) : !type_exports.IsFunction(right) ? ExtendsResult.False : left.parameters.length > right.parameters.length ? ExtendsResult.False : !left.parameters.every((schema, index) => IntoBooleanResult(Visit3(right.parameters[index], schema)) === ExtendsResult.True) ? ExtendsResult.False : IntoBooleanResult(Visit3(left.returns, right.returns));
  }
  function FromIntegerRight(left, right) {
    return type_exports.IsLiteral(left) && value_exports.IsNumber(left.const) ? ExtendsResult.True : type_exports.IsNumber(left) || type_exports.IsInteger(left) ? ExtendsResult.True : ExtendsResult.False;
  }
  function FromInteger(left, right) {
    return type_exports.IsInteger(right) || type_exports.IsNumber(right) ? ExtendsResult.True : IsStructuralRight(right) ? StructuralRight(left, right) : type_exports.IsObject(right) ? FromObjectRight(left, right) : type_exports.IsRecord(right) ? FromRecordRight(left, right) : ExtendsResult.False;
  }
  function FromIntersectRight(left, right) {
    return right.allOf.every((schema) => Visit3(left, schema) === ExtendsResult.True) ? ExtendsResult.True : ExtendsResult.False;
  }
  function FromIntersect4(left, right) {
    return left.allOf.some((schema) => Visit3(schema, right) === ExtendsResult.True) ? ExtendsResult.True : ExtendsResult.False;
  }
  function FromIterator(left, right) {
    return IsStructuralRight(right) ? StructuralRight(left, right) : !type_exports.IsIterator(right) ? ExtendsResult.False : IntoBooleanResult(Visit3(left.items, right.items));
  }
  function FromLiteral2(left, right) {
    return type_exports.IsLiteral(right) && right.const === left.const ? ExtendsResult.True : IsStructuralRight(right) ? StructuralRight(left, right) : type_exports.IsObject(right) ? FromObjectRight(left, right) : type_exports.IsRecord(right) ? FromRecordRight(left, right) : type_exports.IsString(right) ? FromStringRight(left, right) : type_exports.IsNumber(right) ? FromNumberRight(left, right) : type_exports.IsInteger(right) ? FromIntegerRight(left, right) : type_exports.IsBoolean(right) ? FromBooleanRight(left, right) : ExtendsResult.False;
  }
  function FromNeverRight(left, right) {
    return ExtendsResult.False;
  }
  function FromNever(left, right) {
    return ExtendsResult.True;
  }
  function UnwrapTNot(schema) {
    let [current, depth] = [schema, 0];
    while (true) {
      if (!type_exports.IsNot(current))
        break;
      current = current.not;
      depth += 1;
    }
    return depth % 2 === 0 ? current : Unknown();
  }
  function FromNot(left, right) {
    return type_exports.IsNot(left) ? Visit3(UnwrapTNot(left), right) : type_exports.IsNot(right) ? Visit3(left, UnwrapTNot(right)) : Throw("Invalid fallthrough for Not");
  }
  function FromNull(left, right) {
    return IsStructuralRight(right) ? StructuralRight(left, right) : type_exports.IsObject(right) ? FromObjectRight(left, right) : type_exports.IsRecord(right) ? FromRecordRight(left, right) : type_exports.IsNull(right) ? ExtendsResult.True : ExtendsResult.False;
  }
  function FromNumberRight(left, right) {
    return type_exports.IsLiteralNumber(left) ? ExtendsResult.True : type_exports.IsNumber(left) || type_exports.IsInteger(left) ? ExtendsResult.True : ExtendsResult.False;
  }
  function FromNumber(left, right) {
    return IsStructuralRight(right) ? StructuralRight(left, right) : type_exports.IsObject(right) ? FromObjectRight(left, right) : type_exports.IsRecord(right) ? FromRecordRight(left, right) : type_exports.IsInteger(right) || type_exports.IsNumber(right) ? ExtendsResult.True : ExtendsResult.False;
  }
  function IsObjectPropertyCount(schema, count) {
    return Object.getOwnPropertyNames(schema.properties).length === count;
  }
  function IsObjectStringLike(schema) {
    return IsObjectArrayLike(schema);
  }
  function IsObjectSymbolLike(schema) {
    return IsObjectPropertyCount(schema, 0) || IsObjectPropertyCount(schema, 1) && "description" in schema.properties && type_exports.IsUnion(schema.properties.description) && schema.properties.description.anyOf.length === 2 && (type_exports.IsString(schema.properties.description.anyOf[0]) && type_exports.IsUndefined(schema.properties.description.anyOf[1]) || type_exports.IsString(schema.properties.description.anyOf[1]) && type_exports.IsUndefined(schema.properties.description.anyOf[0]));
  }
  function IsObjectNumberLike(schema) {
    return IsObjectPropertyCount(schema, 0);
  }
  function IsObjectBooleanLike(schema) {
    return IsObjectPropertyCount(schema, 0);
  }
  function IsObjectBigIntLike(schema) {
    return IsObjectPropertyCount(schema, 0);
  }
  function IsObjectDateLike(schema) {
    return IsObjectPropertyCount(schema, 0);
  }
  function IsObjectUint8ArrayLike(schema) {
    return IsObjectArrayLike(schema);
  }
  function IsObjectFunctionLike(schema) {
    const length = Number2();
    return IsObjectPropertyCount(schema, 0) || IsObjectPropertyCount(schema, 1) && "length" in schema.properties && IntoBooleanResult(Visit3(schema.properties["length"], length)) === ExtendsResult.True;
  }
  function IsObjectConstructorLike(schema) {
    return IsObjectPropertyCount(schema, 0);
  }
  function IsObjectArrayLike(schema) {
    const length = Number2();
    return IsObjectPropertyCount(schema, 0) || IsObjectPropertyCount(schema, 1) && "length" in schema.properties && IntoBooleanResult(Visit3(schema.properties["length"], length)) === ExtendsResult.True;
  }
  function IsObjectPromiseLike(schema) {
    const then = Function2([Any()], Any());
    return IsObjectPropertyCount(schema, 0) || IsObjectPropertyCount(schema, 1) && "then" in schema.properties && IntoBooleanResult(Visit3(schema.properties["then"], then)) === ExtendsResult.True;
  }
  function Property(left, right) {
    return Visit3(left, right) === ExtendsResult.False ? ExtendsResult.False : type_exports.IsOptional(left) && !type_exports.IsOptional(right) ? ExtendsResult.False : ExtendsResult.True;
  }
  function FromObjectRight(left, right) {
    return type_exports.IsUnknown(left) ? ExtendsResult.False : type_exports.IsAny(left) ? ExtendsResult.Union : type_exports.IsNever(left) || type_exports.IsLiteralString(left) && IsObjectStringLike(right) || type_exports.IsLiteralNumber(left) && IsObjectNumberLike(right) || type_exports.IsLiteralBoolean(left) && IsObjectBooleanLike(right) || type_exports.IsSymbol(left) && IsObjectSymbolLike(right) || type_exports.IsBigInt(left) && IsObjectBigIntLike(right) || type_exports.IsString(left) && IsObjectStringLike(right) || type_exports.IsSymbol(left) && IsObjectSymbolLike(right) || type_exports.IsNumber(left) && IsObjectNumberLike(right) || type_exports.IsInteger(left) && IsObjectNumberLike(right) || type_exports.IsBoolean(left) && IsObjectBooleanLike(right) || type_exports.IsUint8Array(left) && IsObjectUint8ArrayLike(right) || type_exports.IsDate(left) && IsObjectDateLike(right) || type_exports.IsConstructor(left) && IsObjectConstructorLike(right) || type_exports.IsFunction(left) && IsObjectFunctionLike(right) ? ExtendsResult.True : type_exports.IsRecord(left) && type_exports.IsString(RecordKey(left)) ? (() => {
      return right[Hint] === "Record" ? ExtendsResult.True : ExtendsResult.False;
    })() : type_exports.IsRecord(left) && type_exports.IsNumber(RecordKey(left)) ? (() => {
      return IsObjectPropertyCount(right, 0) ? ExtendsResult.True : ExtendsResult.False;
    })() : ExtendsResult.False;
  }
  function FromObject(left, right) {
    return IsStructuralRight(right) ? StructuralRight(left, right) : type_exports.IsRecord(right) ? FromRecordRight(left, right) : !type_exports.IsObject(right) ? ExtendsResult.False : (() => {
      for (const key of Object.getOwnPropertyNames(right.properties)) {
        if (!(key in left.properties) && !type_exports.IsOptional(right.properties[key])) {
          return ExtendsResult.False;
        }
        if (type_exports.IsOptional(right.properties[key])) {
          return ExtendsResult.True;
        }
        if (Property(left.properties[key], right.properties[key]) === ExtendsResult.False) {
          return ExtendsResult.False;
        }
      }
      return ExtendsResult.True;
    })();
  }
  function FromPromise2(left, right) {
    return IsStructuralRight(right) ? StructuralRight(left, right) : type_exports.IsObject(right) && IsObjectPromiseLike(right) ? ExtendsResult.True : !type_exports.IsPromise(right) ? ExtendsResult.False : IntoBooleanResult(Visit3(left.item, right.item));
  }
  function RecordKey(schema) {
    return PatternNumberExact in schema.patternProperties ? Number2() : PatternStringExact in schema.patternProperties ? String2() : Throw("Unknown record key pattern");
  }
  function RecordValue(schema) {
    return PatternNumberExact in schema.patternProperties ? schema.patternProperties[PatternNumberExact] : PatternStringExact in schema.patternProperties ? schema.patternProperties[PatternStringExact] : Throw("Unable to get record value schema");
  }
  function FromRecordRight(left, right) {
    const [Key, Value] = [RecordKey(right), RecordValue(right)];
    return type_exports.IsLiteralString(left) && type_exports.IsNumber(Key) && IntoBooleanResult(Visit3(left, Value)) === ExtendsResult.True ? ExtendsResult.True : type_exports.IsUint8Array(left) && type_exports.IsNumber(Key) ? Visit3(left, Value) : type_exports.IsString(left) && type_exports.IsNumber(Key) ? Visit3(left, Value) : type_exports.IsArray(left) && type_exports.IsNumber(Key) ? Visit3(left, Value) : type_exports.IsObject(left) ? (() => {
      for (const key of Object.getOwnPropertyNames(left.properties)) {
        if (Property(Value, left.properties[key]) === ExtendsResult.False) {
          return ExtendsResult.False;
        }
      }
      return ExtendsResult.True;
    })() : ExtendsResult.False;
  }
  function FromRecord(left, right) {
    return IsStructuralRight(right) ? StructuralRight(left, right) : type_exports.IsObject(right) ? FromObjectRight(left, right) : !type_exports.IsRecord(right) ? ExtendsResult.False : Visit3(RecordValue(left), RecordValue(right));
  }
  function FromRegExp(left, right) {
    const L = type_exports.IsRegExp(left) ? String2() : left;
    const R = type_exports.IsRegExp(right) ? String2() : right;
    return Visit3(L, R);
  }
  function FromStringRight(left, right) {
    return type_exports.IsLiteral(left) && value_exports.IsString(left.const) ? ExtendsResult.True : type_exports.IsString(left) ? ExtendsResult.True : ExtendsResult.False;
  }
  function FromString(left, right) {
    return IsStructuralRight(right) ? StructuralRight(left, right) : type_exports.IsObject(right) ? FromObjectRight(left, right) : type_exports.IsRecord(right) ? FromRecordRight(left, right) : type_exports.IsString(right) ? ExtendsResult.True : ExtendsResult.False;
  }
  function FromSymbol(left, right) {
    return IsStructuralRight(right) ? StructuralRight(left, right) : type_exports.IsObject(right) ? FromObjectRight(left, right) : type_exports.IsRecord(right) ? FromRecordRight(left, right) : type_exports.IsSymbol(right) ? ExtendsResult.True : ExtendsResult.False;
  }
  function FromTemplateLiteral2(left, right) {
    return type_exports.IsTemplateLiteral(left) ? Visit3(TemplateLiteralToUnion(left), right) : type_exports.IsTemplateLiteral(right) ? Visit3(left, TemplateLiteralToUnion(right)) : Throw("Invalid fallthrough for TemplateLiteral");
  }
  function IsArrayOfTuple(left, right) {
    return type_exports.IsArray(right) && left.items !== void 0 && left.items.every((schema) => Visit3(schema, right.items) === ExtendsResult.True);
  }
  function FromTupleRight(left, right) {
    return type_exports.IsNever(left) ? ExtendsResult.True : type_exports.IsUnknown(left) ? ExtendsResult.False : type_exports.IsAny(left) ? ExtendsResult.Union : ExtendsResult.False;
  }
  function FromTuple3(left, right) {
    return IsStructuralRight(right) ? StructuralRight(left, right) : type_exports.IsObject(right) && IsObjectArrayLike(right) ? ExtendsResult.True : type_exports.IsArray(right) && IsArrayOfTuple(left, right) ? ExtendsResult.True : !type_exports.IsTuple(right) ? ExtendsResult.False : value_exports.IsUndefined(left.items) && !value_exports.IsUndefined(right.items) || !value_exports.IsUndefined(left.items) && value_exports.IsUndefined(right.items) ? ExtendsResult.False : value_exports.IsUndefined(left.items) && !value_exports.IsUndefined(right.items) ? ExtendsResult.True : left.items.every((schema, index) => Visit3(schema, right.items[index]) === ExtendsResult.True) ? ExtendsResult.True : ExtendsResult.False;
  }
  function FromUint8Array(left, right) {
    return IsStructuralRight(right) ? StructuralRight(left, right) : type_exports.IsObject(right) ? FromObjectRight(left, right) : type_exports.IsRecord(right) ? FromRecordRight(left, right) : type_exports.IsUint8Array(right) ? ExtendsResult.True : ExtendsResult.False;
  }
  function FromUndefined(left, right) {
    return IsStructuralRight(right) ? StructuralRight(left, right) : type_exports.IsObject(right) ? FromObjectRight(left, right) : type_exports.IsRecord(right) ? FromRecordRight(left, right) : type_exports.IsVoid(right) ? FromVoidRight(left, right) : type_exports.IsUndefined(right) ? ExtendsResult.True : ExtendsResult.False;
  }
  function FromUnionRight(left, right) {
    return right.anyOf.some((schema) => Visit3(left, schema) === ExtendsResult.True) ? ExtendsResult.True : ExtendsResult.False;
  }
  function FromUnion6(left, right) {
    return left.anyOf.every((schema) => Visit3(schema, right) === ExtendsResult.True) ? ExtendsResult.True : ExtendsResult.False;
  }
  function FromUnknownRight(left, right) {
    return ExtendsResult.True;
  }
  function FromUnknown(left, right) {
    return type_exports.IsNever(right) ? FromNeverRight(left, right) : type_exports.IsIntersect(right) ? FromIntersectRight(left, right) : type_exports.IsUnion(right) ? FromUnionRight(left, right) : type_exports.IsAny(right) ? FromAnyRight(left, right) : type_exports.IsString(right) ? FromStringRight(left, right) : type_exports.IsNumber(right) ? FromNumberRight(left, right) : type_exports.IsInteger(right) ? FromIntegerRight(left, right) : type_exports.IsBoolean(right) ? FromBooleanRight(left, right) : type_exports.IsArray(right) ? FromArrayRight(left, right) : type_exports.IsTuple(right) ? FromTupleRight(left, right) : type_exports.IsObject(right) ? FromObjectRight(left, right) : type_exports.IsUnknown(right) ? ExtendsResult.True : ExtendsResult.False;
  }
  function FromVoidRight(left, right) {
    return type_exports.IsUndefined(left) ? ExtendsResult.True : type_exports.IsUndefined(left) ? ExtendsResult.True : ExtendsResult.False;
  }
  function FromVoid(left, right) {
    return type_exports.IsIntersect(right) ? FromIntersectRight(left, right) : type_exports.IsUnion(right) ? FromUnionRight(left, right) : type_exports.IsUnknown(right) ? FromUnknownRight(left, right) : type_exports.IsAny(right) ? FromAnyRight(left, right) : type_exports.IsObject(right) ? FromObjectRight(left, right) : type_exports.IsVoid(right) ? ExtendsResult.True : ExtendsResult.False;
  }
  function Visit3(left, right) {
    return (
      // resolvable
      type_exports.IsTemplateLiteral(left) || type_exports.IsTemplateLiteral(right) ? FromTemplateLiteral2(left, right) : type_exports.IsRegExp(left) || type_exports.IsRegExp(right) ? FromRegExp(left, right) : type_exports.IsNot(left) || type_exports.IsNot(right) ? FromNot(left, right) : (
        // standard
        type_exports.IsAny(left) ? FromAny(left, right) : type_exports.IsArray(left) ? FromArray4(left, right) : type_exports.IsBigInt(left) ? FromBigInt(left, right) : type_exports.IsBoolean(left) ? FromBoolean(left, right) : type_exports.IsAsyncIterator(left) ? FromAsyncIterator(left, right) : type_exports.IsConstructor(left) ? FromConstructor(left, right) : type_exports.IsDate(left) ? FromDate(left, right) : type_exports.IsFunction(left) ? FromFunction(left, right) : type_exports.IsInteger(left) ? FromInteger(left, right) : type_exports.IsIntersect(left) ? FromIntersect4(left, right) : type_exports.IsIterator(left) ? FromIterator(left, right) : type_exports.IsLiteral(left) ? FromLiteral2(left, right) : type_exports.IsNever(left) ? FromNever(left, right) : type_exports.IsNull(left) ? FromNull(left, right) : type_exports.IsNumber(left) ? FromNumber(left, right) : type_exports.IsObject(left) ? FromObject(left, right) : type_exports.IsRecord(left) ? FromRecord(left, right) : type_exports.IsString(left) ? FromString(left, right) : type_exports.IsSymbol(left) ? FromSymbol(left, right) : type_exports.IsTuple(left) ? FromTuple3(left, right) : type_exports.IsPromise(left) ? FromPromise2(left, right) : type_exports.IsUint8Array(left) ? FromUint8Array(left, right) : type_exports.IsUndefined(left) ? FromUndefined(left, right) : type_exports.IsUnion(left) ? FromUnion6(left, right) : type_exports.IsUnknown(left) ? FromUnknown(left, right) : type_exports.IsVoid(left) ? FromVoid(left, right) : Throw(`Unknown left type operand '${left[Kind]}'`)
      )
    );
  }
  function ExtendsCheck(left, right) {
    return Visit3(left, right);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/extends/extends-from-mapped-result.mjs
  function FromProperties8(P, Right, True, False, options) {
    const Acc = {};
    for (const K2 of globalThis.Object.getOwnPropertyNames(P))
      Acc[K2] = Extends(P[K2], Right, True, False, Clone(options));
    return Acc;
  }
  function FromMappedResult6(Left, Right, True, False, options) {
    return FromProperties8(Left.properties, Right, True, False, options);
  }
  function ExtendsFromMappedResult(Left, Right, True, False, options) {
    const P = FromMappedResult6(Left, Right, True, False, options);
    return MappedResult(P);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/extends/extends.mjs
  function ExtendsResolve(left, right, trueType, falseType) {
    const R = ExtendsCheck(left, right);
    return R === ExtendsResult.Union ? Union([trueType, falseType]) : R === ExtendsResult.True ? trueType : falseType;
  }
  function Extends(L, R, T, F, options) {
    return IsMappedResult(L) ? ExtendsFromMappedResult(L, R, T, F, options) : IsMappedKey(L) ? CreateType(ExtendsFromMappedKey(L, R, T, F, options)) : CreateType(ExtendsResolve(L, R, T, F), options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/extends/extends-from-mapped-key.mjs
  function FromPropertyKey(K, U, L, R, options) {
    return {
      [K]: Extends(Literal(K), U, L, R, Clone(options))
    };
  }
  function FromPropertyKeys(K, U, L, R, options) {
    return K.reduce((Acc, LK) => {
      return { ...Acc, ...FromPropertyKey(LK, U, L, R, options) };
    }, {});
  }
  function FromMappedKey2(K, U, L, R, options) {
    return FromPropertyKeys(K.keys, U, L, R, options);
  }
  function ExtendsFromMappedKey(T, U, L, R, options) {
    const P = FromMappedKey2(T, U, L, R, options);
    return MappedResult(P);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/extends/extends-undefined.mjs
  function Intersect2(schema) {
    return schema.allOf.every((schema2) => ExtendsUndefinedCheck(schema2));
  }
  function Union2(schema) {
    return schema.anyOf.some((schema2) => ExtendsUndefinedCheck(schema2));
  }
  function Not(schema) {
    return !ExtendsUndefinedCheck(schema.not);
  }
  function ExtendsUndefinedCheck(schema) {
    return schema[Kind] === "Intersect" ? Intersect2(schema) : schema[Kind] === "Union" ? Union2(schema) : schema[Kind] === "Not" ? Not(schema) : schema[Kind] === "Undefined" ? true : false;
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/exclude/exclude-from-template-literal.mjs
  function ExcludeFromTemplateLiteral(L, R) {
    return Exclude(TemplateLiteralToUnion(L), R);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/exclude/exclude.mjs
  function ExcludeRest(L, R) {
    const excluded = L.filter((inner) => ExtendsCheck(inner, R) === ExtendsResult.False);
    return excluded.length === 1 ? excluded[0] : Union(excluded);
  }
  function Exclude(L, R, options = {}) {
    if (IsTemplateLiteral(L))
      return CreateType(ExcludeFromTemplateLiteral(L, R), options);
    if (IsMappedResult(L))
      return CreateType(ExcludeFromMappedResult(L, R), options);
    return CreateType(IsUnion(L) ? ExcludeRest(L.anyOf, R) : ExtendsCheck(L, R) !== ExtendsResult.False ? Never() : L, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/exclude/exclude-from-mapped-result.mjs
  function FromProperties9(P, U) {
    const Acc = {};
    for (const K2 of globalThis.Object.getOwnPropertyNames(P))
      Acc[K2] = Exclude(P[K2], U);
    return Acc;
  }
  function FromMappedResult7(R, T) {
    return FromProperties9(R.properties, T);
  }
  function ExcludeFromMappedResult(R, T) {
    const P = FromMappedResult7(R, T);
    return MappedResult(P);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/extract/extract-from-template-literal.mjs
  function ExtractFromTemplateLiteral(L, R) {
    return Extract(TemplateLiteralToUnion(L), R);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/extract/extract.mjs
  function ExtractRest(L, R) {
    const extracted = L.filter((inner) => ExtendsCheck(inner, R) !== ExtendsResult.False);
    return extracted.length === 1 ? extracted[0] : Union(extracted);
  }
  function Extract(L, R, options) {
    if (IsTemplateLiteral(L))
      return CreateType(ExtractFromTemplateLiteral(L, R), options);
    if (IsMappedResult(L))
      return CreateType(ExtractFromMappedResult(L, R), options);
    return CreateType(IsUnion(L) ? ExtractRest(L.anyOf, R) : ExtendsCheck(L, R) !== ExtendsResult.False ? L : Never(), options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/extract/extract-from-mapped-result.mjs
  function FromProperties10(P, T) {
    const Acc = {};
    for (const K2 of globalThis.Object.getOwnPropertyNames(P))
      Acc[K2] = Extract(P[K2], T);
    return Acc;
  }
  function FromMappedResult8(R, T) {
    return FromProperties10(R.properties, T);
  }
  function ExtractFromMappedResult(R, T) {
    const P = FromMappedResult8(R, T);
    return MappedResult(P);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/instance-type/instance-type.mjs
  function InstanceType(schema, options) {
    return IsConstructor(schema) ? CreateType(schema.returns, options) : Never(options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/readonly-optional/readonly-optional.mjs
  function ReadonlyOptional(schema) {
    return Readonly(Optional(schema));
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/record/record.mjs
  function RecordCreateFromPattern(pattern, T, options) {
    return CreateType({ [Kind]: "Record", type: "object", patternProperties: { [pattern]: T } }, options);
  }
  function RecordCreateFromKeys(K, T, options) {
    const result = {};
    for (const K2 of K)
      result[K2] = T;
    return Object2(result, { ...options, [Hint]: "Record" });
  }
  function FromTemplateLiteralKey(K, T, options) {
    return IsTemplateLiteralFinite(K) ? RecordCreateFromKeys(IndexPropertyKeys(K), T, options) : RecordCreateFromPattern(K.pattern, T, options);
  }
  function FromUnionKey(key, type, options) {
    return RecordCreateFromKeys(IndexPropertyKeys(Union(key)), type, options);
  }
  function FromLiteralKey(key, type, options) {
    return RecordCreateFromKeys([key.toString()], type, options);
  }
  function FromRegExpKey(key, type, options) {
    return RecordCreateFromPattern(key.source, type, options);
  }
  function FromStringKey(key, type, options) {
    const pattern = IsUndefined(key.pattern) ? PatternStringExact : key.pattern;
    return RecordCreateFromPattern(pattern, type, options);
  }
  function FromAnyKey(_, type, options) {
    return RecordCreateFromPattern(PatternStringExact, type, options);
  }
  function FromNeverKey(_key, type, options) {
    return RecordCreateFromPattern(PatternNeverExact, type, options);
  }
  function FromBooleanKey(_key, type, options) {
    return Object2({ true: type, false: type }, options);
  }
  function FromIntegerKey(_key, type, options) {
    return RecordCreateFromPattern(PatternNumberExact, type, options);
  }
  function FromNumberKey(_, type, options) {
    return RecordCreateFromPattern(PatternNumberExact, type, options);
  }
  function Record(key, type, options = {}) {
    return IsUnion(key) ? FromUnionKey(key.anyOf, type, options) : IsTemplateLiteral(key) ? FromTemplateLiteralKey(key, type, options) : IsLiteral(key) ? FromLiteralKey(key.const, type, options) : IsBoolean3(key) ? FromBooleanKey(key, type, options) : IsInteger2(key) ? FromIntegerKey(key, type, options) : IsNumber3(key) ? FromNumberKey(key, type, options) : IsRegExp2(key) ? FromRegExpKey(key, type, options) : IsString3(key) ? FromStringKey(key, type, options) : IsAny(key) ? FromAnyKey(key, type, options) : IsNever(key) ? FromNeverKey(key, type, options) : Never(options);
  }
  function RecordPattern(record) {
    return globalThis.Object.getOwnPropertyNames(record.patternProperties)[0];
  }
  function RecordKey2(type) {
    const pattern = RecordPattern(type);
    return pattern === PatternStringExact ? String2() : pattern === PatternNumberExact ? Number2() : String2({ pattern });
  }
  function RecordValue2(type) {
    return type.patternProperties[RecordPattern(type)];
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/instantiate/instantiate.mjs
  function FromConstructor2(args, type) {
    type.parameters = FromTypes(args, type.parameters);
    type.returns = FromType(args, type.returns);
    return type;
  }
  function FromFunction2(args, type) {
    type.parameters = FromTypes(args, type.parameters);
    type.returns = FromType(args, type.returns);
    return type;
  }
  function FromIntersect5(args, type) {
    type.allOf = FromTypes(args, type.allOf);
    return type;
  }
  function FromUnion7(args, type) {
    type.anyOf = FromTypes(args, type.anyOf);
    return type;
  }
  function FromTuple4(args, type) {
    if (IsUndefined(type.items))
      return type;
    type.items = FromTypes(args, type.items);
    return type;
  }
  function FromArray5(args, type) {
    type.items = FromType(args, type.items);
    return type;
  }
  function FromAsyncIterator2(args, type) {
    type.items = FromType(args, type.items);
    return type;
  }
  function FromIterator2(args, type) {
    type.items = FromType(args, type.items);
    return type;
  }
  function FromPromise3(args, type) {
    type.item = FromType(args, type.item);
    return type;
  }
  function FromObject2(args, type) {
    const mappedProperties = FromProperties11(args, type.properties);
    return { ...type, ...Object2(mappedProperties) };
  }
  function FromRecord2(args, type) {
    const mappedKey = FromType(args, RecordKey2(type));
    const mappedValue = FromType(args, RecordValue2(type));
    const result = Record(mappedKey, mappedValue);
    return { ...type, ...result };
  }
  function FromArgument(args, argument) {
    return argument.index in args ? args[argument.index] : Unknown();
  }
  function FromProperty2(args, type) {
    const isReadonly = IsReadonly(type);
    const isOptional = IsOptional(type);
    const mapped = FromType(args, type);
    return isReadonly && isOptional ? ReadonlyOptional(mapped) : isReadonly && !isOptional ? Readonly(mapped) : !isReadonly && isOptional ? Optional(mapped) : mapped;
  }
  function FromProperties11(args, properties) {
    return globalThis.Object.getOwnPropertyNames(properties).reduce((result, key) => {
      return { ...result, [key]: FromProperty2(args, properties[key]) };
    }, {});
  }
  function FromTypes(args, types) {
    return types.map((type) => FromType(args, type));
  }
  function FromType(args, type) {
    return IsConstructor(type) ? FromConstructor2(args, type) : IsFunction3(type) ? FromFunction2(args, type) : IsIntersect(type) ? FromIntersect5(args, type) : IsUnion(type) ? FromUnion7(args, type) : IsTuple(type) ? FromTuple4(args, type) : IsArray3(type) ? FromArray5(args, type) : IsAsyncIterator3(type) ? FromAsyncIterator2(args, type) : IsIterator3(type) ? FromIterator2(args, type) : IsPromise2(type) ? FromPromise3(args, type) : IsObject3(type) ? FromObject2(args, type) : IsRecord(type) ? FromRecord2(args, type) : IsArgument(type) ? FromArgument(args, type) : type;
  }
  function Instantiate(type, args) {
    return FromType(args, CloneType(type));
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/integer/integer.mjs
  function Integer(options) {
    return CreateType({ [Kind]: "Integer", type: "integer" }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/intrinsic/intrinsic-from-mapped-key.mjs
  function MappedIntrinsicPropertyKey(K, M, options) {
    return {
      [K]: Intrinsic(Literal(K), M, Clone(options))
    };
  }
  function MappedIntrinsicPropertyKeys(K, M, options) {
    const result = K.reduce((Acc, L) => {
      return { ...Acc, ...MappedIntrinsicPropertyKey(L, M, options) };
    }, {});
    return result;
  }
  function MappedIntrinsicProperties(T, M, options) {
    return MappedIntrinsicPropertyKeys(T["keys"], M, options);
  }
  function IntrinsicFromMappedKey(T, M, options) {
    const P = MappedIntrinsicProperties(T, M, options);
    return MappedResult(P);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/intrinsic/intrinsic.mjs
  function ApplyUncapitalize(value) {
    const [first, rest] = [value.slice(0, 1), value.slice(1)];
    return [first.toLowerCase(), rest].join("");
  }
  function ApplyCapitalize(value) {
    const [first, rest] = [value.slice(0, 1), value.slice(1)];
    return [first.toUpperCase(), rest].join("");
  }
  function ApplyUppercase(value) {
    return value.toUpperCase();
  }
  function ApplyLowercase(value) {
    return value.toLowerCase();
  }
  function FromTemplateLiteral3(schema, mode, options) {
    const expression = TemplateLiteralParseExact(schema.pattern);
    const finite = IsTemplateLiteralExpressionFinite(expression);
    if (!finite)
      return { ...schema, pattern: FromLiteralValue(schema.pattern, mode) };
    const strings = [...TemplateLiteralExpressionGenerate(expression)];
    const literals = strings.map((value) => Literal(value));
    const mapped = FromRest5(literals, mode);
    const union = Union(mapped);
    return TemplateLiteral([union], options);
  }
  function FromLiteralValue(value, mode) {
    return typeof value === "string" ? mode === "Uncapitalize" ? ApplyUncapitalize(value) : mode === "Capitalize" ? ApplyCapitalize(value) : mode === "Uppercase" ? ApplyUppercase(value) : mode === "Lowercase" ? ApplyLowercase(value) : value : value.toString();
  }
  function FromRest5(T, M) {
    return T.map((L) => Intrinsic(L, M));
  }
  function Intrinsic(schema, mode, options = {}) {
    return (
      // Intrinsic-Mapped-Inference
      IsMappedKey(schema) ? IntrinsicFromMappedKey(schema, mode, options) : (
        // Standard-Inference
        IsTemplateLiteral(schema) ? FromTemplateLiteral3(schema, mode, options) : IsUnion(schema) ? Union(FromRest5(schema.anyOf, mode), options) : IsLiteral(schema) ? Literal(FromLiteralValue(schema.const, mode), options) : (
          // Default Type
          CreateType(schema, options)
        )
      )
    );
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/intrinsic/capitalize.mjs
  function Capitalize(T, options = {}) {
    return Intrinsic(T, "Capitalize", options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/intrinsic/lowercase.mjs
  function Lowercase(T, options = {}) {
    return Intrinsic(T, "Lowercase", options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/intrinsic/uncapitalize.mjs
  function Uncapitalize(T, options = {}) {
    return Intrinsic(T, "Uncapitalize", options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/intrinsic/uppercase.mjs
  function Uppercase(T, options = {}) {
    return Intrinsic(T, "Uppercase", options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/omit/omit-from-mapped-result.mjs
  function FromProperties12(properties, propertyKeys, options) {
    const result = {};
    for (const K2 of globalThis.Object.getOwnPropertyNames(properties))
      result[K2] = Omit(properties[K2], propertyKeys, Clone(options));
    return result;
  }
  function FromMappedResult9(mappedResult, propertyKeys, options) {
    return FromProperties12(mappedResult.properties, propertyKeys, options);
  }
  function OmitFromMappedResult(mappedResult, propertyKeys, options) {
    const properties = FromMappedResult9(mappedResult, propertyKeys, options);
    return MappedResult(properties);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/omit/omit.mjs
  function FromIntersect6(types, propertyKeys) {
    return types.map((type) => OmitResolve(type, propertyKeys));
  }
  function FromUnion8(types, propertyKeys) {
    return types.map((type) => OmitResolve(type, propertyKeys));
  }
  function FromProperty3(properties, key) {
    const { [key]: _, ...R } = properties;
    return R;
  }
  function FromProperties13(properties, propertyKeys) {
    return propertyKeys.reduce((T, K2) => FromProperty3(T, K2), properties);
  }
  function FromObject3(type, propertyKeys, properties) {
    const options = Discard(type, [TransformKind, "$id", "required", "properties"]);
    const mappedProperties = FromProperties13(properties, propertyKeys);
    return Object2(mappedProperties, options);
  }
  function UnionFromPropertyKeys(propertyKeys) {
    const result = propertyKeys.reduce((result2, key) => IsLiteralValue(key) ? [...result2, Literal(key)] : result2, []);
    return Union(result);
  }
  function OmitResolve(type, propertyKeys) {
    return IsIntersect(type) ? Intersect(FromIntersect6(type.allOf, propertyKeys)) : IsUnion(type) ? Union(FromUnion8(type.anyOf, propertyKeys)) : IsObject3(type) ? FromObject3(type, propertyKeys, type.properties) : Object2({});
  }
  function Omit(type, key, options) {
    const typeKey = IsArray(key) ? UnionFromPropertyKeys(key) : key;
    const propertyKeys = IsSchema(key) ? IndexPropertyKeys(key) : key;
    const isTypeRef = IsRef(type);
    const isKeyRef = IsRef(key);
    return IsMappedResult(type) ? OmitFromMappedResult(type, propertyKeys, options) : IsMappedKey(key) ? OmitFromMappedKey(type, key, options) : isTypeRef && isKeyRef ? Computed("Omit", [type, typeKey], options) : !isTypeRef && isKeyRef ? Computed("Omit", [type, typeKey], options) : isTypeRef && !isKeyRef ? Computed("Omit", [type, typeKey], options) : CreateType({ ...OmitResolve(type, propertyKeys), ...options });
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/omit/omit-from-mapped-key.mjs
  function FromPropertyKey2(type, key, options) {
    return { [key]: Omit(type, [key], Clone(options)) };
  }
  function FromPropertyKeys2(type, propertyKeys, options) {
    return propertyKeys.reduce((Acc, LK) => {
      return { ...Acc, ...FromPropertyKey2(type, LK, options) };
    }, {});
  }
  function FromMappedKey3(type, mappedKey, options) {
    return FromPropertyKeys2(type, mappedKey.keys, options);
  }
  function OmitFromMappedKey(type, mappedKey, options) {
    const properties = FromMappedKey3(type, mappedKey, options);
    return MappedResult(properties);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/pick/pick-from-mapped-result.mjs
  function FromProperties14(properties, propertyKeys, options) {
    const result = {};
    for (const K2 of globalThis.Object.getOwnPropertyNames(properties))
      result[K2] = Pick(properties[K2], propertyKeys, Clone(options));
    return result;
  }
  function FromMappedResult10(mappedResult, propertyKeys, options) {
    return FromProperties14(mappedResult.properties, propertyKeys, options);
  }
  function PickFromMappedResult(mappedResult, propertyKeys, options) {
    const properties = FromMappedResult10(mappedResult, propertyKeys, options);
    return MappedResult(properties);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/pick/pick.mjs
  function FromIntersect7(types, propertyKeys) {
    return types.map((type) => PickResolve(type, propertyKeys));
  }
  function FromUnion9(types, propertyKeys) {
    return types.map((type) => PickResolve(type, propertyKeys));
  }
  function FromProperties15(properties, propertyKeys) {
    const result = {};
    for (const K2 of propertyKeys)
      if (K2 in properties)
        result[K2] = properties[K2];
    return result;
  }
  function FromObject4(Type2, keys, properties) {
    const options = Discard(Type2, [TransformKind, "$id", "required", "properties"]);
    const mappedProperties = FromProperties15(properties, keys);
    return Object2(mappedProperties, options);
  }
  function UnionFromPropertyKeys2(propertyKeys) {
    const result = propertyKeys.reduce((result2, key) => IsLiteralValue(key) ? [...result2, Literal(key)] : result2, []);
    return Union(result);
  }
  function PickResolve(type, propertyKeys) {
    return IsIntersect(type) ? Intersect(FromIntersect7(type.allOf, propertyKeys)) : IsUnion(type) ? Union(FromUnion9(type.anyOf, propertyKeys)) : IsObject3(type) ? FromObject4(type, propertyKeys, type.properties) : Object2({});
  }
  function Pick(type, key, options) {
    const typeKey = IsArray(key) ? UnionFromPropertyKeys2(key) : key;
    const propertyKeys = IsSchema(key) ? IndexPropertyKeys(key) : key;
    const isTypeRef = IsRef(type);
    const isKeyRef = IsRef(key);
    return IsMappedResult(type) ? PickFromMappedResult(type, propertyKeys, options) : IsMappedKey(key) ? PickFromMappedKey(type, key, options) : isTypeRef && isKeyRef ? Computed("Pick", [type, typeKey], options) : !isTypeRef && isKeyRef ? Computed("Pick", [type, typeKey], options) : isTypeRef && !isKeyRef ? Computed("Pick", [type, typeKey], options) : CreateType({ ...PickResolve(type, propertyKeys), ...options });
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/pick/pick-from-mapped-key.mjs
  function FromPropertyKey3(type, key, options) {
    return {
      [key]: Pick(type, [key], Clone(options))
    };
  }
  function FromPropertyKeys3(type, propertyKeys, options) {
    return propertyKeys.reduce((result, leftKey) => {
      return { ...result, ...FromPropertyKey3(type, leftKey, options) };
    }, {});
  }
  function FromMappedKey4(type, mappedKey, options) {
    return FromPropertyKeys3(type, mappedKey.keys, options);
  }
  function PickFromMappedKey(type, mappedKey, options) {
    const properties = FromMappedKey4(type, mappedKey, options);
    return MappedResult(properties);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/partial/partial.mjs
  function FromComputed3(target, parameters) {
    return Computed("Partial", [Computed(target, parameters)]);
  }
  function FromRef3($ref) {
    return Computed("Partial", [Ref($ref)]);
  }
  function FromProperties16(properties) {
    const partialProperties = {};
    for (const K of globalThis.Object.getOwnPropertyNames(properties))
      partialProperties[K] = Optional(properties[K]);
    return partialProperties;
  }
  function FromObject5(type, properties) {
    const options = Discard(type, [TransformKind, "$id", "required", "properties"]);
    const mappedProperties = FromProperties16(properties);
    return Object2(mappedProperties, options);
  }
  function FromRest6(types) {
    return types.map((type) => PartialResolve(type));
  }
  function PartialResolve(type) {
    return (
      // Mappable
      IsComputed(type) ? FromComputed3(type.target, type.parameters) : IsRef(type) ? FromRef3(type.$ref) : IsIntersect(type) ? Intersect(FromRest6(type.allOf)) : IsUnion(type) ? Union(FromRest6(type.anyOf)) : IsObject3(type) ? FromObject5(type, type.properties) : (
        // Intrinsic
        IsBigInt3(type) ? type : IsBoolean3(type) ? type : IsInteger2(type) ? type : IsLiteral(type) ? type : IsNull3(type) ? type : IsNumber3(type) ? type : IsString3(type) ? type : IsSymbol3(type) ? type : IsUndefined3(type) ? type : (
          // Passthrough
          Object2({})
        )
      )
    );
  }
  function Partial(type, options) {
    if (IsMappedResult(type)) {
      return PartialFromMappedResult(type, options);
    } else {
      return CreateType({ ...PartialResolve(type), ...options });
    }
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/partial/partial-from-mapped-result.mjs
  function FromProperties17(K, options) {
    const Acc = {};
    for (const K2 of globalThis.Object.getOwnPropertyNames(K))
      Acc[K2] = Partial(K[K2], Clone(options));
    return Acc;
  }
  function FromMappedResult11(R, options) {
    return FromProperties17(R.properties, options);
  }
  function PartialFromMappedResult(R, options) {
    const P = FromMappedResult11(R, options);
    return MappedResult(P);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/required/required.mjs
  function FromComputed4(target, parameters) {
    return Computed("Required", [Computed(target, parameters)]);
  }
  function FromRef4($ref) {
    return Computed("Required", [Ref($ref)]);
  }
  function FromProperties18(properties) {
    const requiredProperties = {};
    for (const K of globalThis.Object.getOwnPropertyNames(properties))
      requiredProperties[K] = Discard(properties[K], [OptionalKind]);
    return requiredProperties;
  }
  function FromObject6(type, properties) {
    const options = Discard(type, [TransformKind, "$id", "required", "properties"]);
    const mappedProperties = FromProperties18(properties);
    return Object2(mappedProperties, options);
  }
  function FromRest7(types) {
    return types.map((type) => RequiredResolve(type));
  }
  function RequiredResolve(type) {
    return (
      // Mappable
      IsComputed(type) ? FromComputed4(type.target, type.parameters) : IsRef(type) ? FromRef4(type.$ref) : IsIntersect(type) ? Intersect(FromRest7(type.allOf)) : IsUnion(type) ? Union(FromRest7(type.anyOf)) : IsObject3(type) ? FromObject6(type, type.properties) : (
        // Intrinsic
        IsBigInt3(type) ? type : IsBoolean3(type) ? type : IsInteger2(type) ? type : IsLiteral(type) ? type : IsNull3(type) ? type : IsNumber3(type) ? type : IsString3(type) ? type : IsSymbol3(type) ? type : IsUndefined3(type) ? type : (
          // Passthrough
          Object2({})
        )
      )
    );
  }
  function Required(type, options) {
    if (IsMappedResult(type)) {
      return RequiredFromMappedResult(type, options);
    } else {
      return CreateType({ ...RequiredResolve(type), ...options });
    }
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/required/required-from-mapped-result.mjs
  function FromProperties19(P, options) {
    const Acc = {};
    for (const K2 of globalThis.Object.getOwnPropertyNames(P))
      Acc[K2] = Required(P[K2], options);
    return Acc;
  }
  function FromMappedResult12(R, options) {
    return FromProperties19(R.properties, options);
  }
  function RequiredFromMappedResult(R, options) {
    const P = FromMappedResult12(R, options);
    return MappedResult(P);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/module/compute.mjs
  function DereferenceParameters(moduleProperties, types) {
    return types.map((type) => {
      return IsRef(type) ? Dereference(moduleProperties, type.$ref) : FromType2(moduleProperties, type);
    });
  }
  function Dereference(moduleProperties, ref) {
    return ref in moduleProperties ? IsRef(moduleProperties[ref]) ? Dereference(moduleProperties, moduleProperties[ref].$ref) : FromType2(moduleProperties, moduleProperties[ref]) : Never();
  }
  function FromAwaited(parameters) {
    return Awaited(parameters[0]);
  }
  function FromIndex(parameters) {
    return Index(parameters[0], parameters[1]);
  }
  function FromKeyOf(parameters) {
    return KeyOf(parameters[0]);
  }
  function FromPartial(parameters) {
    return Partial(parameters[0]);
  }
  function FromOmit(parameters) {
    return Omit(parameters[0], parameters[1]);
  }
  function FromPick(parameters) {
    return Pick(parameters[0], parameters[1]);
  }
  function FromRequired(parameters) {
    return Required(parameters[0]);
  }
  function FromComputed5(moduleProperties, target, parameters) {
    const dereferenced = DereferenceParameters(moduleProperties, parameters);
    return target === "Awaited" ? FromAwaited(dereferenced) : target === "Index" ? FromIndex(dereferenced) : target === "KeyOf" ? FromKeyOf(dereferenced) : target === "Partial" ? FromPartial(dereferenced) : target === "Omit" ? FromOmit(dereferenced) : target === "Pick" ? FromPick(dereferenced) : target === "Required" ? FromRequired(dereferenced) : Never();
  }
  function FromArray6(moduleProperties, type) {
    return Array2(FromType2(moduleProperties, type));
  }
  function FromAsyncIterator3(moduleProperties, type) {
    return AsyncIterator(FromType2(moduleProperties, type));
  }
  function FromConstructor3(moduleProperties, parameters, instanceType) {
    return Constructor(FromTypes2(moduleProperties, parameters), FromType2(moduleProperties, instanceType));
  }
  function FromFunction3(moduleProperties, parameters, returnType) {
    return Function2(FromTypes2(moduleProperties, parameters), FromType2(moduleProperties, returnType));
  }
  function FromIntersect8(moduleProperties, types) {
    return Intersect(FromTypes2(moduleProperties, types));
  }
  function FromIterator3(moduleProperties, type) {
    return Iterator(FromType2(moduleProperties, type));
  }
  function FromObject7(moduleProperties, properties) {
    return Object2(globalThis.Object.keys(properties).reduce((result, key) => {
      return { ...result, [key]: FromType2(moduleProperties, properties[key]) };
    }, {}));
  }
  function FromRecord3(moduleProperties, type) {
    const [value, pattern] = [FromType2(moduleProperties, RecordValue2(type)), RecordPattern(type)];
    const result = CloneType(type);
    result.patternProperties[pattern] = value;
    return result;
  }
  function FromTransform(moduleProperties, transform) {
    return IsRef(transform) ? { ...Dereference(moduleProperties, transform.$ref), [TransformKind]: transform[TransformKind] } : transform;
  }
  function FromTuple5(moduleProperties, types) {
    return Tuple(FromTypes2(moduleProperties, types));
  }
  function FromUnion10(moduleProperties, types) {
    return Union(FromTypes2(moduleProperties, types));
  }
  function FromTypes2(moduleProperties, types) {
    return types.map((type) => FromType2(moduleProperties, type));
  }
  function FromType2(moduleProperties, type) {
    return (
      // Modifiers
      IsOptional(type) ? CreateType(FromType2(moduleProperties, Discard(type, [OptionalKind])), type) : IsReadonly(type) ? CreateType(FromType2(moduleProperties, Discard(type, [ReadonlyKind])), type) : (
        // Transform
        IsTransform(type) ? CreateType(FromTransform(moduleProperties, type), type) : (
          // Types
          IsArray3(type) ? CreateType(FromArray6(moduleProperties, type.items), type) : IsAsyncIterator3(type) ? CreateType(FromAsyncIterator3(moduleProperties, type.items), type) : IsComputed(type) ? CreateType(FromComputed5(moduleProperties, type.target, type.parameters)) : IsConstructor(type) ? CreateType(FromConstructor3(moduleProperties, type.parameters, type.returns), type) : IsFunction3(type) ? CreateType(FromFunction3(moduleProperties, type.parameters, type.returns), type) : IsIntersect(type) ? CreateType(FromIntersect8(moduleProperties, type.allOf), type) : IsIterator3(type) ? CreateType(FromIterator3(moduleProperties, type.items), type) : IsObject3(type) ? CreateType(FromObject7(moduleProperties, type.properties), type) : IsRecord(type) ? CreateType(FromRecord3(moduleProperties, type)) : IsTuple(type) ? CreateType(FromTuple5(moduleProperties, type.items || []), type) : IsUnion(type) ? CreateType(FromUnion10(moduleProperties, type.anyOf), type) : type
        )
      )
    );
  }
  function ComputeType(moduleProperties, key) {
    return key in moduleProperties ? FromType2(moduleProperties, moduleProperties[key]) : Never();
  }
  function ComputeModuleProperties(moduleProperties) {
    return globalThis.Object.getOwnPropertyNames(moduleProperties).reduce((result, key) => {
      return { ...result, [key]: ComputeType(moduleProperties, key) };
    }, {});
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/module/module.mjs
  var TModule = class {
    constructor($defs) {
      const computed = ComputeModuleProperties($defs);
      const identified = this.WithIdentifiers(computed);
      this.$defs = identified;
    }
    /** `[Json]` Imports a Type by Key. */
    Import(key, options) {
      const $defs = { ...this.$defs, [key]: CreateType(this.$defs[key], options) };
      return CreateType({ [Kind]: "Import", $defs, $ref: key });
    }
    // prettier-ignore
    WithIdentifiers($defs) {
      return globalThis.Object.getOwnPropertyNames($defs).reduce((result, key) => {
        return { ...result, [key]: { ...$defs[key], $id: key } };
      }, {});
    }
  };
  function Module(properties) {
    return new TModule(properties);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/not/not.mjs
  function Not2(type, options) {
    return CreateType({ [Kind]: "Not", not: type }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/parameters/parameters.mjs
  function Parameters(schema, options) {
    return IsFunction3(schema) ? Tuple(schema.parameters, options) : Never();
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/recursive/recursive.mjs
  var Ordinal = 0;
  function Recursive(callback, options = {}) {
    if (IsUndefined(options.$id))
      options.$id = `T${Ordinal++}`;
    const thisType = CloneType(callback({ [Kind]: "This", $ref: `${options.$id}` }));
    thisType.$id = options.$id;
    return CreateType({ [Hint]: "Recursive", ...thisType }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/regexp/regexp.mjs
  function RegExp2(unresolved, options) {
    const expr = IsString(unresolved) ? new globalThis.RegExp(unresolved) : unresolved;
    return CreateType({ [Kind]: "RegExp", type: "RegExp", source: expr.source, flags: expr.flags }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/rest/rest.mjs
  function RestResolve(T) {
    return IsIntersect(T) ? T.allOf : IsUnion(T) ? T.anyOf : IsTuple(T) ? T.items ?? [] : [];
  }
  function Rest(T) {
    return RestResolve(T);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/return-type/return-type.mjs
  function ReturnType(schema, options) {
    return IsFunction3(schema) ? CreateType(schema.returns, options) : Never(options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/transform/transform.mjs
  var TransformDecodeBuilder = class {
    constructor(schema) {
      this.schema = schema;
    }
    Decode(decode) {
      return new TransformEncodeBuilder(this.schema, decode);
    }
  };
  var TransformEncodeBuilder = class {
    constructor(schema, decode) {
      this.schema = schema;
      this.decode = decode;
    }
    EncodeTransform(encode, schema) {
      const Encode2 = (value) => schema[TransformKind].Encode(encode(value));
      const Decode2 = (value) => this.decode(schema[TransformKind].Decode(value));
      const Codec = { Encode: Encode2, Decode: Decode2 };
      return { ...schema, [TransformKind]: Codec };
    }
    EncodeSchema(encode, schema) {
      const Codec = { Decode: this.decode, Encode: encode };
      return { ...schema, [TransformKind]: Codec };
    }
    Encode(encode) {
      return IsTransform(this.schema) ? this.EncodeTransform(encode, this.schema) : this.EncodeSchema(encode, this.schema);
    }
  };
  function Transform(schema) {
    return new TransformDecodeBuilder(schema);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/unsafe/unsafe.mjs
  function Unsafe(options = {}) {
    return CreateType({ [Kind]: options[Kind] ?? "Unsafe" }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/void/void.mjs
  function Void(options) {
    return CreateType({ [Kind]: "Void", type: "void" }, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/type/type/type.mjs
  var type_exports3 = {};
  __export(type_exports3, {
    Any: () => Any,
    Argument: () => Argument,
    Array: () => Array2,
    AsyncIterator: () => AsyncIterator,
    Awaited: () => Awaited,
    BigInt: () => BigInt2,
    Boolean: () => Boolean2,
    Capitalize: () => Capitalize,
    Composite: () => Composite,
    Const: () => Const,
    Constructor: () => Constructor,
    ConstructorParameters: () => ConstructorParameters,
    Date: () => Date2,
    Enum: () => Enum,
    Exclude: () => Exclude,
    Extends: () => Extends,
    Extract: () => Extract,
    Function: () => Function2,
    Index: () => Index,
    InstanceType: () => InstanceType,
    Instantiate: () => Instantiate,
    Integer: () => Integer,
    Intersect: () => Intersect,
    Iterator: () => Iterator,
    KeyOf: () => KeyOf,
    Literal: () => Literal,
    Lowercase: () => Lowercase,
    Mapped: () => Mapped,
    Module: () => Module,
    Never: () => Never,
    Not: () => Not2,
    Null: () => Null,
    Number: () => Number2,
    Object: () => Object2,
    Omit: () => Omit,
    Optional: () => Optional,
    Parameters: () => Parameters,
    Partial: () => Partial,
    Pick: () => Pick,
    Promise: () => Promise2,
    Readonly: () => Readonly,
    ReadonlyOptional: () => ReadonlyOptional,
    Record: () => Record,
    Recursive: () => Recursive,
    Ref: () => Ref,
    RegExp: () => RegExp2,
    Required: () => Required,
    Rest: () => Rest,
    ReturnType: () => ReturnType,
    String: () => String2,
    Symbol: () => Symbol2,
    TemplateLiteral: () => TemplateLiteral,
    Transform: () => Transform,
    Tuple: () => Tuple,
    Uint8Array: () => Uint8Array2,
    Uncapitalize: () => Uncapitalize,
    Undefined: () => Undefined,
    Union: () => Union,
    Unknown: () => Unknown,
    Unsafe: () => Unsafe,
    Uppercase: () => Uppercase,
    Void: () => Void
  });

  // ../../node_modules/@sinclair/typebox/build/esm/type/type/index.mjs
  var Type = type_exports3;

  // ../../packages/ai/dist/api-registry.js
  var apiProviderRegistry = /* @__PURE__ */ new Map();
  function registerApiProvider(provider) {
    apiProviderRegistry.set(provider.api, {
      api: provider.api,
      stream: (model, context, options) => provider.stream(model, context, options),
      streamSimple: (model, context, options) => provider.streamSimple(model, context, options)
    });
  }
  function getApiProvider(api) {
    return apiProviderRegistry.get(api);
  }

  // ../../packages/ai/dist/env-api-keys.js
  var PROVIDER_ENV_MAP = {
    "kimi-cn": "KIMI_API_KEY",
    "bigmodel": "BIGMODEL_API_KEY",
    "openai": "OPENAI_API_KEY",
    "anthropic": "ANTHROPIC_API_KEY",
    "openrouter": "OPENROUTER_API_KEY",
    google: "GOOGLE_API_KEY",
    gemini: "GOOGLE_API_KEY",
    "cy-gpt": "CY_GPT_API_KEY",
    "cy-gpt2": "CY_GPT2_API_KEY",
    "kimi-api": "KIMI_API_KEY",
    "kimi-coding": "KIMI_API_KEY"
  };
  function getProviderEnvVar(provider) {
    return PROVIDER_ENV_MAP[provider] || `${provider.toUpperCase().replace(/-/g, "_")}_API_KEY`;
  }
  function getEnvApiKey(provider) {
    const env = typeof process !== "undefined" ? process.env : {};
    const explicitEnv = getProviderEnvVar(provider);
    if (explicitEnv) {
      const value = env[explicitEnv];
      if (value)
        return value;
    }
    return void 0;
  }

  // ../../packages/ai/dist/models.builtins.js
  var BUILTIN_MODEL_DEFINITIONS = {
    "kimi-cn": {
      "kimi-k2.5": {
        name: "Kimi K2.5",
        api: "openai-completions",
        provider: "kimi-cn",
        baseUrl: "https://api.moonshot.cn/v1",
        reasoning: true,
        input: ["text", "image"],
        cost: { input: 0, output: 0, cacheRead: 0, cacheWrite: 0 },
        contextWindow: 262144,
        maxTokens: 8192
      },
      "kimi-k2": {
        name: "Kimi K2",
        api: "openai-completions",
        provider: "kimi-cn",
        baseUrl: "https://api.moonshot.cn/v1",
        reasoning: false,
        input: ["text"],
        cost: { input: 0, output: 0, cacheRead: 0, cacheWrite: 0 },
        contextWindow: 131072,
        maxTokens: 8192
      },
      "moonshot-v1-128k": {
        name: "Moonshot V1 128K",
        api: "openai-completions",
        provider: "kimi-cn",
        baseUrl: "https://api.moonshot.cn/v1",
        reasoning: false,
        input: ["text"],
        cost: { input: 0, output: 0, cacheRead: 0, cacheWrite: 0 },
        contextWindow: 131072,
        maxTokens: 8192
      },
      "moonshot-v1-32k": {
        name: "Moonshot V1 32K",
        api: "openai-completions",
        provider: "kimi-cn",
        baseUrl: "https://api.moonshot.cn/v1",
        reasoning: false,
        input: ["text"],
        cost: { input: 0, output: 0, cacheRead: 0, cacheWrite: 0 },
        contextWindow: 32768,
        maxTokens: 8192
      }
    },
    bigmodel: {
      "glm-5": {
        name: "GLM-5",
        api: "openai-completions",
        provider: "bigmodel",
        baseUrl: "https://open.bigmodel.cn/api/paas/v4",
        reasoning: true,
        input: ["text"],
        cost: { input: 1, output: 3.2, cacheRead: 0, cacheWrite: 0 },
        contextWindow: 131072,
        maxTokens: 8192,
        compat: {
          supportsDeveloperRole: false
        }
      },
      "glm-5-flash": {
        name: "GLM-5 Flash",
        api: "openai-completions",
        provider: "bigmodel",
        baseUrl: "https://open.bigmodel.cn/api/paas/v4",
        reasoning: false,
        input: ["text"],
        cost: { input: 0.5, output: 1.5, cacheRead: 0, cacheWrite: 0 },
        contextWindow: 131072,
        maxTokens: 8192,
        compat: {
          supportsDeveloperRole: false
        }
      },
      "glm-4-plus": {
        name: "GLM-4 Plus",
        api: "openai-completions",
        provider: "bigmodel",
        baseUrl: "https://open.bigmodel.cn/api/paas/v4",
        reasoning: false,
        input: ["text", "image"],
        cost: { input: 0, output: 0, cacheRead: 0, cacheWrite: 0 },
        contextWindow: 128e3,
        maxTokens: 4096
      }
    }
  };
  var KIMI_MODEL_DEFINITIONS = BUILTIN_MODEL_DEFINITIONS["kimi-cn"];
  var GLM_MODEL_DEFINITIONS = BUILTIN_MODEL_DEFINITIONS.bigmodel;

  // ../../packages/ai/dist/models.js
  function calculateCost(model, usage) {
    const input = model.cost.input / 1e6 * usage.input;
    const output = model.cost.output / 1e6 * usage.output;
    const cacheRead = model.cost.cacheRead / 1e6 * usage.cacheRead;
    const cacheWrite = model.cost.cacheWrite / 1e6 * usage.cacheWrite;
    return {
      input,
      output,
      cacheRead,
      cacheWrite,
      total: input + output + cacheRead + cacheWrite
    };
  }

  // ../../packages/ai/dist/utils/event-stream.js
  var EventStream = class {
    isComplete;
    extractResult;
    queue = [];
    waiting = [];
    done = false;
    resultResolved = false;
    finalResultPromise;
    resolveFinalResult;
    constructor(isComplete, extractResult) {
      this.isComplete = isComplete;
      this.extractResult = extractResult;
      this.finalResultPromise = new Promise((resolve) => {
        this.resolveFinalResult = resolve;
      });
    }
    push(event) {
      if (this.done)
        return;
      if (this.isComplete(event)) {
        this.done = true;
        if (!this.resultResolved) {
          this.resultResolved = true;
          this.resolveFinalResult(this.extractResult(event));
        }
      }
      const waiter = this.waiting.shift();
      if (waiter) {
        waiter({ value: event, done: false });
      } else {
        this.queue.push(event);
      }
    }
    end(result) {
      this.done = true;
      if (result !== void 0 && !this.resultResolved) {
        this.resultResolved = true;
        this.resolveFinalResult(result);
      }
      while (this.waiting.length > 0) {
        const waiter = this.waiting.shift();
        waiter({ value: void 0, done: true });
      }
    }
    async *[Symbol.asyncIterator]() {
      while (true) {
        if (this.queue.length > 0) {
          yield this.queue.shift();
        } else if (this.done) {
          return;
        } else {
          const result = await new Promise((resolve) => this.waiting.push(resolve));
          if (result.done)
            return;
          yield result.value;
        }
      }
    }
    result() {
      return this.finalResultPromise;
    }
  };
  var AssistantMessageEventStream = class extends EventStream {
    constructor() {
      super((event) => event.type === "done" || event.type === "error", (event) => {
        if (event.type === "done") {
          return event.message;
        } else if (event.type === "error") {
          return event.error;
        }
        throw new Error("Unexpected event type for final result");
      });
    }
  };

  // ../../packages/platform/dist/detector.js
  function isPuerTS() {
    try {
      return typeof CS !== "undefined" && CS !== null;
    } catch {
      return false;
    }
  }
  function isNode() {
    try {
      return typeof process !== "undefined" && process.versions != null && process.versions.node != null && !isPuerTS();
    } catch {
      return false;
    }
  }
  function detectPlatform() {
    if (isPuerTS()) {
      return "puerts";
    }
    if (isNode()) {
      return "node";
    }
    return "unknown";
  }

  // node-stub:node:module
  function createRequire(filename) {
    return function require5(id) {
      return {};
    };
  }

  // node-stub:node:url
  function fileURLToPath(url) {
    return url ? String(url).replace(/^file:\/\//, "") : "/";
  }

  // ../../packages/platform/dist/node-fs.js
  var __filename = fileURLToPath("file:///pie/core.js");
  var require2 = createRequire(__filename);
  var nodeFs = null;
  var nodePath = null;
  function getNodeFs() {
    if (!nodeFs) {
      nodeFs = require2("fs");
    }
    return nodeFs;
  }
  function getNodePath() {
    if (!nodePath) {
      nodePath = require2("path");
    }
    return nodePath;
  }
  function convertStats(stats) {
    return {
      name: "",
      // Will be set by caller
      isDirectory: stats.isDirectory(),
      isFile: stats.isFile(),
      size: stats.size,
      mtime: stats.mtime
    };
  }
  var NodeFileSystem = class {
    platform = "node";
    sep;
    constructor() {
      this.platform = detectPlatform();
      this.sep = getNodePath().sep;
    }
    // File operations
    async readFile(path, encoding = "utf-8") {
      return getNodeFs().promises.readFile(path, encoding);
    }
    readFileSync(path, encoding = "utf-8") {
      return getNodeFs().readFileSync(path, encoding);
    }
    async writeFile(path, data, encoding = "utf-8") {
      return getNodeFs().promises.writeFile(path, data, encoding);
    }
    writeFileSync(path, data, encoding = "utf-8") {
      getNodeFs().writeFileSync(path, data, encoding);
    }
    // Directory operations
    async mkdir(path, options) {
      await getNodeFs().promises.mkdir(path, options);
    }
    mkdirSync(path, options) {
      getNodeFs().mkdirSync(path, options);
    }
    async readdir(path) {
      return getNodeFs().promises.readdir(path);
    }
    readdirSync(path) {
      return getNodeFs().readdirSync(path);
    }
    // Path checks
    async exists(path) {
      try {
        await getNodeFs().promises.access(path);
        return true;
      } catch {
        return false;
      }
    }
    existsSync(path) {
      return getNodeFs().existsSync(path);
    }
    async stat(path) {
      const stats = await getNodeFs().promises.stat(path);
      const entry = convertStats(stats);
      entry.name = getNodePath().basename(path);
      return entry;
    }
    statSync(path) {
      const stats = getNodeFs().statSync(path);
      const entry = convertStats(stats);
      entry.name = getNodePath().basename(path);
      return entry;
    }
    async access(path, mode) {
      await getNodeFs().promises.access(path, mode);
    }
    // File manipulation
    async unlink(path) {
      await getNodeFs().promises.unlink(path);
    }
    unlinkSync(path) {
      getNodeFs().unlinkSync(path);
    }
    // Path utilities - delegate to Node.js path module
    join(...paths) {
      return getNodePath().join(...paths);
    }
    dirname(path) {
      return getNodePath().dirname(path);
    }
    basename(path, ext) {
      return getNodePath().basename(path, ext);
    }
    extname(path) {
      return getNodePath().extname(path);
    }
    resolve(...paths) {
      return getNodePath().resolve(...paths);
    }
    normalize(path) {
      return getNodePath().normalize(path);
    }
    isAbsolute(path) {
      return getNodePath().isAbsolute(path);
    }
    relative(from, to) {
      return getNodePath().relative(from, to);
    }
  };

  // ../../packages/platform/dist/puerts-fs.js
  function getSystemIO() {
    if (typeof CS === "undefined" || !CS?.System?.IO) {
      throw new Error("CS.System.IO not available - not running in PuerTS environment");
    }
    return CS.System.IO;
  }
  function convertDateTime(dateTime) {
    if (!dateTime)
      return /* @__PURE__ */ new Date(0);
    const ticks = dateTime.get_Ticks?.() || 0;
    const epochTicks = 621355968e9;
    const ms = (ticks - epochTicks) / 1e4;
    return new Date(ms);
  }
  function convertFileSystemInfo(info) {
    if (!info) {
      return {
        name: "",
        isDirectory: false,
        isFile: false,
        size: 0,
        mtime: /* @__PURE__ */ new Date(0)
      };
    }
    const isDir = info.Attributes !== void 0 ? (info.Attributes & getSystemIO().FileAttributes.Directory) !== 0 : false;
    return {
      name: info.Name || "",
      isDirectory: isDir,
      isFile: !isDir,
      size: info.Length || 0,
      mtime: info.LastWriteTimeUtc ? convertDateTime(info.LastWriteTimeUtc) : /* @__PURE__ */ new Date(0)
    };
  }
  var PuerTSFileSystem = class {
    platform = "puerts";
    sep = "/";
    constructor() {
      this.sep = "/";
    }
    normalizePath(path) {
      return path.replace(/\\/g, "/");
    }
    // File operations
    async readFile(path, encoding = "utf-8") {
      const IO = getSystemIO();
      const normalizedPath = this.normalizePath(path);
      if (!IO.File.Exists(normalizedPath)) {
        throw new Error(`ENOENT: no such file or directory, open '${path}'`);
      }
      return IO.File.ReadAllText(normalizedPath);
    }
    readFileSync(path, encoding = "utf-8") {
      const IO = getSystemIO();
      const normalizedPath = this.normalizePath(path);
      if (!IO.File.Exists(normalizedPath)) {
        throw new Error(`ENOENT: no such file or directory, open '${path}'`);
      }
      return IO.File.ReadAllText(normalizedPath);
    }
    async writeFile(path, data, encoding = "utf-8") {
      const IO = getSystemIO();
      const normalizedPath = this.normalizePath(path);
      const dir = IO.Path.GetDirectoryName(normalizedPath);
      if (dir && !IO.Directory.Exists(dir)) {
        IO.Directory.CreateDirectory(dir);
      }
      IO.File.WriteAllText(normalizedPath, data);
    }
    writeFileSync(path, data, encoding = "utf-8") {
      const IO = getSystemIO();
      const normalizedPath = this.normalizePath(path);
      const dir = IO.Path.GetDirectoryName(normalizedPath);
      if (dir && !IO.Directory.Exists(dir)) {
        IO.Directory.CreateDirectory(dir);
      }
      IO.File.WriteAllText(normalizedPath, data);
    }
    // Directory operations
    async mkdir(path, options) {
      const IO = getSystemIO();
      const normalizedPath = this.normalizePath(path);
      if (IO.Directory.Exists(normalizedPath)) {
        return;
      }
      if (options?.recursive) {
        IO.Directory.CreateDirectory(normalizedPath);
      } else {
        const parent = IO.Path.GetDirectoryName(normalizedPath);
        if (parent && !IO.Directory.Exists(parent)) {
          throw new Error(`ENOENT: no such file or directory, mkdir '${path}'`);
        }
        IO.Directory.CreateDirectory(normalizedPath);
      }
    }
    mkdirSync(path, options) {
      const IO = getSystemIO();
      const normalizedPath = this.normalizePath(path);
      if (IO.Directory.Exists(normalizedPath)) {
        return;
      }
      if (options?.recursive) {
        IO.Directory.CreateDirectory(normalizedPath);
      } else {
        const parent = IO.Path.GetDirectoryName(normalizedPath);
        if (parent && !IO.Directory.Exists(parent)) {
          throw new Error(`ENOENT: no such file or directory, mkdir '${path}'`);
        }
        IO.Directory.CreateDirectory(normalizedPath);
      }
    }
    async readdir(path) {
      const IO = getSystemIO();
      const normalizedPath = this.normalizePath(path);
      if (!IO.Directory.Exists(normalizedPath)) {
        throw new Error(`ENOENT: no such file or directory, scandir '${path}'`);
      }
      const entries = [];
      const files = IO.Directory.GetFiles(normalizedPath);
      for (let i = 0; i < files.Length; i++) {
        entries.push(IO.Path.GetFileName(files.get_Item(i)));
      }
      const dirs = IO.Directory.GetDirectories(normalizedPath);
      for (let i = 0; i < dirs.Length; i++) {
        entries.push(IO.Path.GetFileName(dirs.get_Item(i)));
      }
      return entries;
    }
    readdirSync(path) {
      const IO = getSystemIO();
      const normalizedPath = this.normalizePath(path);
      if (!IO.Directory.Exists(normalizedPath)) {
        throw new Error(`ENOENT: no such file or directory, scandir '${path}'`);
      }
      const entries = [];
      const files = IO.Directory.GetFiles(normalizedPath);
      for (let i = 0; i < files.Length; i++) {
        entries.push(IO.Path.GetFileName(files.get_Item(i)));
      }
      const dirs = IO.Directory.GetDirectories(normalizedPath);
      for (let i = 0; i < dirs.Length; i++) {
        entries.push(IO.Path.GetFileName(dirs.get_Item(i)));
      }
      return entries;
    }
    // Path checks
    async exists(path) {
      const IO = getSystemIO();
      const normalizedPath = this.normalizePath(path);
      return IO.File.Exists(normalizedPath) || IO.Directory.Exists(normalizedPath);
    }
    existsSync(path) {
      const IO = getSystemIO();
      const normalizedPath = this.normalizePath(path);
      return IO.File.Exists(normalizedPath) || IO.Directory.Exists(normalizedPath);
    }
    async stat(path) {
      const IO = getSystemIO();
      const normalizedPath = this.normalizePath(path);
      if (IO.File.Exists(normalizedPath)) {
        const info = new IO.FileInfo(normalizedPath);
        return convertFileSystemInfo(info);
      }
      if (IO.Directory.Exists(normalizedPath)) {
        const info = new IO.DirectoryInfo(normalizedPath);
        return convertFileSystemInfo(info);
      }
      throw new Error(`ENOENT: no such file or directory, stat '${path}'`);
    }
    statSync(path) {
      const IO = getSystemIO();
      const normalizedPath = this.normalizePath(path);
      if (IO.File.Exists(normalizedPath)) {
        const info = new IO.FileInfo(normalizedPath);
        return convertFileSystemInfo(info);
      }
      if (IO.Directory.Exists(normalizedPath)) {
        const info = new IO.DirectoryInfo(normalizedPath);
        return convertFileSystemInfo(info);
      }
      throw new Error(`ENOENT: no such file or directory, stat '${path}'`);
    }
    async access(path, mode) {
      const IO = getSystemIO();
      const normalizedPath = this.normalizePath(path);
      if (!IO.File.Exists(normalizedPath) && !IO.Directory.Exists(normalizedPath)) {
        throw new Error(`ENOENT: no such file or directory, access '${path}'`);
      }
    }
    // File manipulation
    async unlink(path) {
      const IO = getSystemIO();
      const normalizedPath = this.normalizePath(path);
      if (IO.File.Exists(normalizedPath)) {
        IO.File.Delete(normalizedPath);
      } else {
        throw new Error(`ENOENT: no such file or directory, unlink '${path}'`);
      }
    }
    unlinkSync(path) {
      const IO = getSystemIO();
      const normalizedPath = this.normalizePath(path);
      if (IO.File.Exists(normalizedPath)) {
        IO.File.Delete(normalizedPath);
      } else {
        throw new Error(`ENOENT: no such file or directory, unlink '${path}'`);
      }
    }
    // Path utilities - pure JavaScript implementation
    join(...paths) {
      return paths.map((p) => p.replace(/\\/g, "/")).join("/").replace(/\/+/g, "/");
    }
    dirname(path) {
      const normalized = path.replace(/\\/g, "/");
      const lastSlash = normalized.lastIndexOf("/");
      if (lastSlash === -1)
        return ".";
      if (lastSlash === 0)
        return "/";
      return normalized.slice(0, lastSlash);
    }
    basename(path, ext) {
      const normalized = path.replace(/\\/g, "/");
      const lastSlash = normalized.lastIndexOf("/");
      const name = lastSlash === -1 ? normalized : normalized.slice(lastSlash + 1);
      if (ext && name.endsWith(ext)) {
        return name.slice(0, -ext.length);
      }
      return name;
    }
    extname(path) {
      const base = this.basename(path);
      const lastDot = base.lastIndexOf(".");
      if (lastDot === -1 || lastDot === 0)
        return "";
      return base.slice(lastDot);
    }
    resolve(...paths) {
      let resolved = "";
      for (const p of paths) {
        const normalized = p.replace(/\\/g, "/");
        if (this.isAbsolute(normalized)) {
          resolved = normalized;
        } else {
          resolved = resolved ? this.join(resolved, normalized) : normalized;
        }
      }
      return resolved || ".";
    }
    normalize(path) {
      const normalized = path.replace(/\\/g, "/");
      const parts = normalized.split("/");
      const result = [];
      for (const part of parts) {
        if (part === "..") {
          if (result.length > 0 && result[result.length - 1] !== "..") {
            result.pop();
          } else {
            result.push("..");
          }
        } else if (part !== "." && part !== "") {
          result.push(part);
        }
      }
      const joined = result.join("/");
      return normalized.startsWith("/") ? "/" + joined : joined;
    }
    isAbsolute(path) {
      const normalized = path.replace(/\\/g, "/");
      return normalized.startsWith("/") || /^[a-zA-Z]:/.test(normalized);
    }
    relative(from, to) {
      const fromNormalized = this.normalize(from);
      const toNormalized = this.normalize(to);
      const fromParts = fromNormalized.split("/").filter((p) => p);
      const toParts = toNormalized.split("/").filter((p) => p);
      let commonIndex = 0;
      while (commonIndex < fromParts.length && commonIndex < toParts.length && fromParts[commonIndex] === toParts[commonIndex]) {
        commonIndex++;
      }
      const upCount = fromParts.length - commonIndex;
      const result = [...Array(upCount).fill(".."), ...toParts.slice(commonIndex)];
      return result.join("/") || ".";
    }
  };

  // node-stub:node:fs
  var appendFileSync = () => {
  };
  var existsSync = () => false;
  var mkdirSync = () => {
  };

  // node-stub:node:path
  var join = (...p) => p.filter(Boolean).join("/").replace(/\/+/g, "/");

  // ../../packages/platform/dist/http.js
  var __filename2 = fileURLToPath("file:///pie/core.js");
  var require3 = createRequire(__filename2);
  var _procEnv = typeof process !== "undefined" ? process.env : {};
  var DEBUG = _procEnv.PIE_DEBUG_HTTP === "1" || _procEnv.PIE_DEBUG_HTTP === "true";
  var LOG_TO_FILE = _procEnv.PIE_DEBUG_HTTP_FILE === "1" || _procEnv.PIE_DEBUG_HTTP_FILE === "true";
  var logFilePath = null;
  function isTuiSessionActive() {
    return _procEnv.PIE_TUI_ACTIVE === "1" || _procEnv.PIE_TUI_ACTIVE === "true";
  }
  function initLogFile() {
    if (!LOG_TO_FILE)
      return null;
    try {
      const homeDir = _procEnv.HOME || _procEnv.USERPROFILE || "/tmp";
      const logDir = join(homeDir, ".pie", "logs");
      if (!existsSync(logDir)) {
        mkdirSync(logDir, { recursive: true });
      }
      logFilePath = join(logDir, `http-${(/* @__PURE__ */ new Date()).toISOString().split("T")[0]}.log`);
      return logFilePath;
    } catch {
      return null;
    }
  }
  function logHttp(level, message, data) {
    const timestamp = (/* @__PURE__ */ new Date()).toISOString();
    const prefix = `[${timestamp}] [HTTP:${level}]`;
    const logLine = data ? `${prefix} ${message} ${JSON.stringify(data)}` : `${prefix} ${message}`;
    if (level === "ERROR" && !isTuiSessionActive()) {
      console.error(logLine);
    } else if (DEBUG) {
      console.error(logLine);
    }
    if (LOG_TO_FILE && logFilePath) {
      try {
        appendFileSync(logFilePath, logLine + "\n");
      } catch {
      }
    }
  }
  function isAbortErrorMessage(message) {
    return message === "Request was aborted" || message === "Operation aborted";
  }
  initLogFile();
  var nodeHttps = null;
  var nodeHttp = null;
  var nodeUrl = null;
  function getNodeHttps() {
    if (!nodeHttps) {
      nodeHttps = require3("https");
    }
    return nodeHttps;
  }
  function getNodeHttp() {
    if (!nodeHttp) {
      nodeHttp = require3("http");
    }
    return nodeHttp;
  }
  function getNodeUrl() {
    if (!nodeUrl) {
      nodeUrl = require3("url");
    }
    return nodeUrl;
  }
  var globalHttpsAgent = null;
  var globalHttpAgent = null;
  var agentStats = {
    httpsRequests: 0,
    httpsReusedConnections: 0,
    httpRequests: 0,
    httpReusedConnections: 0
  };
  function getHttpsAgent() {
    if (!globalHttpsAgent) {
      const https = getNodeHttps();
      globalHttpsAgent = new https.Agent({
        keepAlive: true,
        maxSockets: 20,
        // 最大并发连接数
        maxFreeSockets: 10,
        // 保持空闲连接数
        timeout: 6e4,
        // 连接超时 60s
        scheduling: "lifo"
        // 最近最少使用，优先复用
      });
      logHttp("INFO", "HTTPS Agent created with keep-alive", {
        maxSockets: 20,
        maxFreeSockets: 10,
        timeout: 6e4
      });
    }
    return globalHttpsAgent;
  }
  function getHttpAgent() {
    if (!globalHttpAgent) {
      const http = getNodeHttp();
      globalHttpAgent = new http.Agent({
        keepAlive: true,
        maxSockets: 20,
        maxFreeSockets: 10,
        timeout: 6e4,
        scheduling: "lifo"
      });
      logHttp("INFO", "HTTP Agent created with keep-alive");
    }
    return globalHttpAgent;
  }
  var NodeHttpClient = class {
    async request(url, options = {}) {
      const requestStartTime = Date.now();
      const requestId = `${Date.now()}-${Math.random().toString(36).slice(2, 7)}`;
      logHttp("DEBUG", `Request [${requestId}] started`, {
        url: url.slice(0, 100),
        method: options.method || "GET",
        hasBody: !!options.body
      });
      return new Promise((resolve, reject) => {
        const parsedUrl = new (getNodeUrl()).URL(url);
        const isHttps = parsedUrl.protocol === "https:";
        const lib = isHttps ? getNodeHttps() : getNodeHttp();
        const agent2 = isHttps ? getHttpsAgent() : getHttpAgent();
        if (isHttps) {
          agentStats.httpsRequests++;
        } else {
          agentStats.httpRequests++;
        }
        const requestOptions = {
          hostname: parsedUrl.hostname,
          port: parsedUrl.port || (isHttps ? 443 : 80),
          path: parsedUrl.pathname + parsedUrl.search,
          method: options.method || "GET",
          headers: options.headers || {},
          agent: agent2
          // Use keep-alive agent
        };
        const connectStartTime = Date.now();
        const req = lib.request(requestOptions, (res) => {
          const connectTime = Date.now() - connectStartTime;
          const socket = res.socket;
          const agentInstance = isHttps ? globalHttpsAgent : globalHttpAgent;
          const hasFreeSockets = agentInstance && agentInstance.freeSockets?.[parsedUrl.hostname];
          const wasReused = connectTime < 50 || !!(hasFreeSockets && socket?.readyState === "open");
          if (wasReused) {
            if (isHttps) {
              agentStats.httpsReusedConnections++;
            } else {
              agentStats.httpReusedConnections++;
            }
          }
          logHttp("DEBUG", `Request [${requestId}] connected`, {
            status: res.statusCode,
            connectTimeMs: connectTime,
            connectionReused: wasReused,
            totalHttpsRequests: agentStats.httpsRequests,
            totalHttpsReused: agentStats.httpsReusedConnections
          });
          const status = res.statusCode || 0;
          const responseHeaders = {};
          for (const [key, value] of Object.entries(res.headers)) {
            if (value)
              responseHeaders[key] = Array.isArray(value) ? value.join(", ") : String(value);
          }
          const response = {
            status,
            statusText: res.statusMessage || "",
            ok: status >= 200 && status < 300,
            headers: responseHeaders,
            text() {
              return new Promise((resolveText, rejectText) => {
                const chunks = [];
                res.on("data", (chunk) => chunks.push(chunk));
                res.on("end", () => {
                  const body = Buffer.concat(chunks).toString("utf-8");
                  logHttp("DEBUG", `Request [${requestId}] body received`, {
                    bodyLength: body.length,
                    totalTimeMs: Date.now() - requestStartTime
                  });
                  resolveText(body);
                });
                res.on("error", (err) => {
                  logHttp("ERROR", `Request [${requestId}] body read error`, { error: err.message });
                  rejectText(err);
                });
              });
            },
            async json() {
              const text = await this.text();
              try {
                return JSON.parse(text);
              } catch (err) {
                logHttp("ERROR", `Request [${requestId}] JSON parse error`, {
                  error: err.message,
                  textPreview: text.slice(0, 200)
                });
                throw err;
              }
            },
            // ═════════════════════════════════════════════════════════════════
            // Optimized SSE streaming with Buffer queue (O(1) operations)
            // ═════════════════════════════════════════════════════════════════
            async *readSSELines(signal) {
              const sseStartTime = Date.now();
              let totalBytes = 0;
              let lineCount = 0;
              let bufferChunks = [];
              let bufferLength = 0;
              const lineQueue = [];
              let queueHead = 0;
              let done = false;
              let error = null;
              let waiting = null;
              const flushBuffer = () => {
                if (bufferLength === 0)
                  return;
                const buffer = Buffer.concat(bufferChunks, bufferLength);
                const bufferStr = buffer.toString("utf-8");
                const lines = bufferStr.split("\n");
                const lastLine = lines.pop();
                if (lastLine !== void 0) {
                  bufferChunks = [Buffer.from(lastLine)];
                  bufferLength = lastLine.length;
                } else {
                  bufferChunks = [];
                  bufferLength = 0;
                }
                for (const line of lines) {
                  lineQueue.push(line);
                }
              };
              if (signal) {
                signal.addEventListener("abort", () => {
                  res.destroy();
                }, { once: true });
              }
              res.on("data", (chunk) => {
                totalBytes += chunk.length;
                bufferChunks.push(chunk);
                bufferLength += chunk.length;
                if (bufferLength > 16384 || chunk.includes(10)) {
                  flushBuffer();
                }
                if (waiting) {
                  const w = waiting;
                  waiting = null;
                  w({ done: false, value: void 0 });
                }
              });
              res.on("end", () => {
                if (bufferLength > 0) {
                  flushBuffer();
                }
                done = true;
                if (waiting) {
                  const w = waiting;
                  waiting = null;
                  w({ done: true, value: void 0 });
                }
                logHttp("DEBUG", `Request [${requestId}] SSE stream ended`, {
                  totalBytes,
                  lineCount,
                  durationMs: Date.now() - sseStartTime
                });
              });
              res.on("error", (e) => {
                error = e;
                done = true;
                if (waiting) {
                  const w = waiting;
                  waiting = null;
                  w({ done: true, value: void 0 });
                }
                logHttp("ERROR", `Request [${requestId}] SSE stream error`, { error: e.message });
              });
              try {
                while (true) {
                  while (queueHead < lineQueue.length) {
                    const line = lineQueue[queueHead++];
                    lineCount++;
                    yield line;
                  }
                  if (queueHead > 1e3) {
                    lineQueue.splice(0, queueHead);
                    queueHead = 0;
                  }
                  if (done) {
                    if (error)
                      throw error;
                    return;
                  }
                  const waitResult = await new Promise((r) => {
                    waiting = r;
                  });
                  if (waitResult.done)
                    break;
                }
              } finally {
                if (!done && !res.destroyed) {
                  res.destroy();
                }
              }
            }
          };
          resolve(response);
        });
        req.on("error", (e) => {
          if (isAbortErrorMessage(e.message) && options.signal?.aborted) {
            logHttp("DEBUG", `Request [${requestId}] aborted`, {
              totalTimeMs: Date.now() - requestStartTime
            });
            reject(e);
            return;
          }
          logHttp("ERROR", `Request [${requestId}] failed`, {
            error: e.message,
            totalTimeMs: Date.now() - requestStartTime
          });
          reject(e);
        });
        req.on("timeout", () => {
          logHttp("ERROR", `Request [${requestId}] timeout`);
          req.destroy(new Error("Request timeout"));
        });
        if (options.signal) {
          options.signal.addEventListener("abort", () => {
            logHttp("DEBUG", `Request [${requestId}] aborted`);
            req.destroy(new Error("Request was aborted"));
          }, { once: true });
        }
        if (options.body) {
          req.write(options.body);
        }
        req.end();
      });
    }
  };
  var PuerTSHttpClient = class {
    async request(url, options = {}) {
      logHttp("DEBUG", `PuerTS Request started`, {
        url: url.slice(0, 100),
        method: options.method || "GET"
      });
      if (typeof CS !== "undefined" && CS?.AgentBridge?.HttpPostAsyncStart && CS?.AgentBridge?.HttpPostAsyncPoll) {
        const method2 = options.method || "GET";
        const body2 = options.body || "";
        const auth = options.headers?.Authorization || options.headers?.authorization || "";
        const authValue = auth.replace(/^Bearer\s+/i, "");
        console.log(`[PuerTSHttpClient] Using async HttpClient for ${url}`);
        const requestId = CS.AgentBridge.HttpPostAsyncStart(url, body2, authValue);
        console.log(`[PuerTSHttpClient] Request started: ${requestId}`);
        const maxWait = 12e4;
        const pollInterval = 50;
        const startTime = Date.now();
        while (true) {
          if (options.signal?.aborted) {
            CS.AgentBridge.HttpPostAsyncCancel(requestId);
            throw new Error("Request was aborted");
          }
          const pollResult = CS.AgentBridge.HttpPostAsyncPoll(requestId);
          console.log(`[PuerTSHttpClient] Poll result type: ${typeof pollResult}, length: ${pollResult?.length || 0}`);
          if (pollResult && pollResult.length > 0) {
            const parsed = JSON.parse(pollResult);
            console.log(`[PuerTSHttpClient] Response status: ${parsed.statusCode}, body length: ${parsed.body?.length || 0}`);
            return {
              status: parsed.statusCode || 200,
              statusText: parsed.status === "ok" ? "OK" : "Error",
              ok: parsed.status === "ok",
              headers: { "content-type": "application/json" },
              text() {
                return Promise.resolve(parsed.body || "");
              },
              async json() {
                return JSON.parse(parsed.body || "{}");
              },
              async *readSSELines(signal) {
                const text = parsed.body || "";
                const lines = text.split("\n");
                for (const line of lines) {
                  if (signal?.aborted)
                    throw new Error("Request was aborted");
                  yield line;
                }
              }
            };
          }
          if (Date.now() - startTime > maxWait) {
            CS.AgentBridge.HttpPostAsyncCancel(requestId);
            throw new Error("Request timed out");
          }
          await new Promise((resolve) => setTimeout(resolve, pollInterval));
        }
      }
      if (typeof CS === "undefined" || !CS?.UnityEngine?.Networking) {
        throw new Error("HTTP client not available - not running in PuerTS environment");
      }
      const { UnityEngine } = CS;
      const method = options.method || "GET";
      const headers = options.headers || {};
      const body = options.body;
      return new Promise((resolve, reject) => {
        const request = new UnityEngine.Networking.UnityWebRequest(url, method);
        for (const [key, value] of Object.entries(headers)) {
          request.SetRequestHeader(key, value);
        }
        if (body && ["POST", "PUT", "PATCH"].includes(method.toUpperCase())) {
          const encoder = new TextEncoder();
          const bodyData = encoder.encode(body);
          request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyData);
        }
        request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
        let aborted = false;
        if (options.signal) {
          const onAbort = () => {
            aborted = true;
            request.Abort();
            reject(new Error("Request was aborted"));
          };
          options.signal.addEventListener("abort", onAbort, { once: true });
        }
        const asyncOp = request.SendWebRequest();
        const checkComplete = () => {
          if (aborted)
            return;
          if (asyncOp.isDone) {
            const status = request.responseCode;
            const ok = status >= 200 && status < 300;
            const responseHeaders = {};
            const headerKeys = request.GetResponseHeaderKeys?.() || [];
            for (const key of headerKeys) {
              const value = request.GetResponseHeader(key);
              if (value)
                responseHeaders[key] = value;
            }
            const responseText = request.downloadHandler?.text || "";
            console.log(`[PuerTSHttpClient] Request completed: status=${status}, ok=${ok}, textLength=${responseText.length}`);
            resolve({
              status,
              statusText: request.error || "",
              ok,
              headers: responseHeaders,
              text() {
                return Promise.resolve(responseText);
              },
              async json() {
                return JSON.parse(responseText);
              },
              // SSE support for PuerTS - parse from complete response
              async *readSSELines(signal) {
                const text = responseText;
                const lines = text.split("\n");
                console.log(`[PuerTSHttpClient] readSSELines: ${lines.length} lines, first 100 chars: ${text.substring(0, 100)}`);
                for (const line of lines) {
                  if (signal?.aborted) {
                    throw new Error("Request was aborted");
                  }
                  yield line;
                }
              }
            });
          } else {
            if (typeof setTimeout !== "undefined") {
              setTimeout(checkComplete, 10);
            } else {
              const startWait = Date.now();
              const wait = () => {
                if (!asyncOp.isDone && Date.now() - startWait < 3e4) {
                  if (typeof setImmediate !== "undefined") {
                    setImmediate(wait);
                  } else {
                    checkComplete();
                  }
                } else {
                  checkComplete();
                }
              };
              wait();
            }
          }
        };
        checkComplete();
      });
    }
  };

  // ../../packages/platform/dist/config.js
  var __filename3 = fileURLToPath("file:///pie/core.js");
  var require4 = createRequire(__filename3);

  // ../../packages/platform/dist/gateway.js
  var __filename4 = fileURLToPath("file:///pie/core.js");
  var nodeRequire = createRequire(__filename4);

  // ../../packages/platform/dist/index.js
  var fileSystemInstance = null;
  var httpClientInstance = null;
  var platformContextInstance = null;
  function getFileSystem() {
    if (!fileSystemInstance) {
      const platform = detectPlatform();
      switch (platform) {
        case "puerts":
          fileSystemInstance = new PuerTSFileSystem();
          break;
        case "node":
        default:
          fileSystemInstance = new NodeFileSystem();
          break;
      }
    }
    return fileSystemInstance;
  }
  function getHttpClient() {
    if (!httpClientInstance) {
      const platform = detectPlatform();
      switch (platform) {
        case "puerts":
          try {
            const testRequire = globalThis.require || __require;
            if (testRequire("https")) {
              httpClientInstance = new NodeHttpClient();
            } else {
              httpClientInstance = new PuerTSHttpClient();
            }
          } catch {
            httpClientInstance = new PuerTSHttpClient();
          }
          break;
        case "node":
        default:
          httpClientInstance = new NodeHttpClient();
          break;
      }
    }
    return httpClientInstance;
  }
  function setHttpClient(client) {
    httpClientInstance = client;
    platformContextInstance = null;
  }

  // ../../packages/ai/dist/utils/http.js
  function httpRequest(url, options = {}) {
    const client = getHttpClient();
    return client.request(url, options);
  }

  // ../../packages/ai/dist/utils/json-parse.js
  var import_partial_json = __toESM(require_dist(), 1);
  function parseStreamingJson(partialJson) {
    if (!partialJson || partialJson.trim() === "") {
      return {};
    }
    try {
      return JSON.parse(partialJson);
    } catch {
      try {
        const result = (0, import_partial_json.parse)(partialJson);
        return result ?? {};
      } catch {
        return {};
      }
    }
  }

  // ../../packages/ai/dist/providers/simple-options.js
  function buildBaseOptions(model, options, apiKey) {
    return {
      temperature: options?.temperature,
      maxTokens: options?.maxTokens || Math.min(model.maxTokens, 32e3),
      signal: options?.signal,
      apiKey: apiKey || options?.apiKey,
      cacheRetention: options?.cacheRetention,
      sessionId: options?.sessionId,
      headers: options?.headers,
      onPayload: options?.onPayload,
      maxRetryDelayMs: options?.maxRetryDelayMs,
      metadata: options?.metadata
    };
  }
  function clampReasoning(effort) {
    return effort === "xhigh" ? "high" : effort;
  }
  function adjustMaxTokensForThinking(baseMaxTokens, modelMaxTokens, reasoningLevel, customBudgets) {
    const defaultBudgets = {
      minimal: 1024,
      low: 2048,
      medium: 8192,
      high: 16384
    };
    const budgets = { ...defaultBudgets, ...customBudgets };
    const minOutputTokens = 1024;
    const level = clampReasoning(reasoningLevel);
    let thinkingBudget = budgets[level];
    const maxTokens = Math.min(baseMaxTokens + thinkingBudget, modelMaxTokens);
    if (maxTokens <= thinkingBudget) {
      thinkingBudget = Math.max(0, maxTokens - minOutputTokens);
    }
    return { maxTokens, thinkingBudget };
  }

  // ../../packages/ai/dist/providers/transform-messages.js
  function transformMessages(messages, model, normalizeToolCallId2) {
    const toolCallIdMap = /* @__PURE__ */ new Map();
    const transformed = messages.map((msg) => {
      if (msg.role === "user")
        return msg;
      if (msg.role === "toolResult") {
        const normalizedId = toolCallIdMap.get(msg.toolCallId);
        if (normalizedId && normalizedId !== msg.toolCallId) {
          return { ...msg, toolCallId: normalizedId };
        }
        return msg;
      }
      if (msg.role === "assistant") {
        const assistantMsg = msg;
        const isSameModel = assistantMsg.provider === model.provider && assistantMsg.api === model.api && assistantMsg.model === model.id;
        const transformedContent = assistantMsg.content.flatMap((block) => {
          if (block.type === "thinking") {
            if (isSameModel && block.thinkingSignature)
              return block;
            if (!block.thinking || block.thinking.trim() === "")
              return [];
            if (isSameModel)
              return block;
            return { type: "text", text: block.thinking };
          }
          if (block.type === "text") {
            if (isSameModel)
              return block;
            return { type: "text", text: block.text };
          }
          if (block.type === "toolCall") {
            const toolCall = block;
            let normalizedToolCall = toolCall;
            if (!isSameModel && toolCall.thoughtSignature) {
              normalizedToolCall = { ...toolCall };
              delete normalizedToolCall.thoughtSignature;
            }
            if (!isSameModel && normalizeToolCallId2) {
              const normalizedId = normalizeToolCallId2(toolCall.id, model, assistantMsg);
              if (normalizedId !== toolCall.id) {
                toolCallIdMap.set(toolCall.id, normalizedId);
                normalizedToolCall = { ...normalizedToolCall, id: normalizedId };
              }
            }
            return normalizedToolCall;
          }
          return block;
        });
        return { ...assistantMsg, content: transformedContent };
      }
      return msg;
    });
    const result = [];
    let pendingToolCalls = [];
    let existingToolResultIds = /* @__PURE__ */ new Set();
    for (let i = 0; i < transformed.length; i++) {
      const msg = transformed[i];
      if (msg.role === "assistant") {
        if (pendingToolCalls.length > 0) {
          for (const tc of pendingToolCalls) {
            if (!existingToolResultIds.has(tc.id)) {
              result.push({
                role: "toolResult",
                toolCallId: tc.id,
                toolName: tc.name,
                content: [{ type: "text", text: "No result provided" }],
                isError: true,
                timestamp: Date.now()
              });
            }
          }
          pendingToolCalls = [];
          existingToolResultIds = /* @__PURE__ */ new Set();
        }
        const assistantMsg = msg;
        if (assistantMsg.stopReason === "error" || assistantMsg.stopReason === "aborted") {
          continue;
        }
        const toolCalls = assistantMsg.content.filter((b) => b.type === "toolCall");
        if (toolCalls.length > 0) {
          pendingToolCalls = toolCalls;
          existingToolResultIds = /* @__PURE__ */ new Set();
        }
        result.push(msg);
      } else if (msg.role === "toolResult") {
        existingToolResultIds.add(msg.toolCallId);
        result.push(msg);
      } else if (msg.role === "user") {
        if (pendingToolCalls.length > 0) {
          for (const tc of pendingToolCalls) {
            if (!existingToolResultIds.has(tc.id)) {
              result.push({
                role: "toolResult",
                toolCallId: tc.id,
                toolName: tc.name,
                content: [{ type: "text", text: "No result provided" }],
                isError: true,
                timestamp: Date.now()
              });
            }
          }
          pendingToolCalls = [];
          existingToolResultIds = /* @__PURE__ */ new Set();
        }
        result.push(msg);
      } else {
        result.push(msg);
      }
    }
    if (pendingToolCalls.length > 0) {
      for (const tc of pendingToolCalls) {
        if (!existingToolResultIds.has(tc.id)) {
          result.push({
            role: "toolResult",
            toolCallId: tc.id,
            toolName: tc.name,
            content: [{ type: "text", text: "No result provided" }],
            isError: true,
            timestamp: Date.now()
          });
        }
      }
    }
    return result;
  }

  // ../../packages/ai/dist/providers/openai-compat.js
  var streamOpenAICompletions = (model, context, options) => {
    const stream = new AssistantMessageEventStream();
    (async () => {
      const output = {
        role: "assistant",
        content: [],
        api: model.api,
        provider: model.provider,
        model: model.id,
        usage: {
          input: 0,
          output: 0,
          cacheRead: 0,
          cacheWrite: 0,
          totalTokens: 0,
          cost: { input: 0, output: 0, cacheRead: 0, cacheWrite: 0, total: 0 }
        },
        stopReason: "stop",
        timestamp: Date.now()
      };
      try {
        const apiKey = options?.apiKey || getEnvApiKey(model.provider) || "";
        if (!apiKey) {
          throw new Error(`No API key for provider: ${model.provider}`);
        }
        const compat = getCompat(model);
        const params = buildParams(model, context, options, compat);
        options?.onPayload?.(params);
        const headers = {
          "Content-Type": "application/json",
          Authorization: `Bearer ${apiKey}`,
          // GLM/bigmodel requires specific accept header for SSE
          ...model.provider === "bigmodel" ? { "Accept": "text/event-stream" } : {},
          ...model.headers || {},
          ...options?.headers || {}
        };
        const response = await httpRequest(`${model.baseUrl}/chat/completions`, {
          method: "POST",
          headers,
          body: JSON.stringify(params),
          signal: options?.signal
        });
        if (!response.ok) {
          const errorBody = await response.text().catch(() => "");
          throw new Error(`API error ${response.status}: ${errorBody}`);
        }
        stream.push({ type: "start", partial: output });
        let currentBlock = null;
        const blocks = output.content;
        const blockIndex = () => blocks.length - 1;
        const finishCurrentBlock = (block) => {
          if (block) {
            if (block.type === "text") {
              stream.push({
                type: "text_end",
                contentIndex: blockIndex(),
                content: block.text,
                partial: output
              });
            } else if (block.type === "thinking") {
              stream.push({
                type: "thinking_end",
                contentIndex: blockIndex(),
                content: block.thinking,
                partial: output
              });
            } else if (block.type === "toolCall") {
              block.arguments = parseStreamingJson(block.partialArgs);
              delete block.partialArgs;
              stream.push({
                type: "toolcall_end",
                contentIndex: blockIndex(),
                toolCall: block,
                partial: output
              });
            }
          }
        };
        if (!response.readSSELines) {
          throw new Error("SSE streaming not supported on this platform");
        }
        for await (const line of response.readSSELines(options?.signal)) {
          {
            const trimmed = line.trim();
            if (!trimmed || trimmed.startsWith(":"))
              continue;
            if (trimmed.startsWith("data: ")) {
              const data = trimmed.slice(6);
              if (data === "[DONE]")
                continue;
              let chunk;
              try {
                chunk = JSON.parse(data);
              } catch {
                continue;
              }
              if (chunk.usage) {
                const cachedTokens = chunk.usage.prompt_tokens_details?.cached_tokens || 0;
                const reasoningTokens = chunk.usage.completion_tokens_details?.reasoning_tokens || 0;
                const input = (chunk.usage.prompt_tokens || 0) - cachedTokens;
                const outputTokens = (chunk.usage.completion_tokens || 0) + reasoningTokens;
                output.usage = {
                  input,
                  output: outputTokens,
                  cacheRead: cachedTokens,
                  cacheWrite: 0,
                  totalTokens: input + outputTokens + cachedTokens,
                  cost: { input: 0, output: 0, cacheRead: 0, cacheWrite: 0, total: 0 }
                };
                calculateCost(model, output.usage);
                if (process.env.PIE_DEBUG_CACHE) {
                  console.error("[CACHE DEBUG] Response usage:", JSON.stringify({
                    input_tokens: input,
                    output_tokens: outputTokens,
                    cached_tokens: cachedTokens,
                    cache_hit_rate: chunk.usage.prompt_tokens ? `${(cachedTokens / chunk.usage.prompt_tokens * 100).toFixed(1)}%` : "N/A"
                  }, null, 2));
                }
              }
              const choice = chunk.choices?.[0];
              if (!choice)
                continue;
              if (choice.finish_reason) {
                output.stopReason = mapStopReason(choice.finish_reason);
              }
              if (choice.delta) {
                if (choice.delta.content !== null && choice.delta.content !== void 0 && choice.delta.content.length > 0) {
                  if (!currentBlock || currentBlock.type !== "text") {
                    finishCurrentBlock(currentBlock);
                    currentBlock = { type: "text", text: "" };
                    output.content.push(currentBlock);
                    stream.push({
                      type: "text_start",
                      contentIndex: blockIndex(),
                      partial: output
                    });
                  }
                  if (currentBlock.type === "text") {
                    currentBlock.text += choice.delta.content;
                    stream.push({
                      type: "text_delta",
                      contentIndex: blockIndex(),
                      delta: choice.delta.content,
                      partial: output
                    });
                  }
                }
                const reasoningFields = [
                  "reasoning_content",
                  "reasoning",
                  "reasoning_text"
                ];
                let foundReasoningField = null;
                for (const field of reasoningFields) {
                  if (choice.delta[field] !== null && choice.delta[field] !== void 0 && choice.delta[field].length > 0) {
                    foundReasoningField = field;
                    break;
                  }
                }
                if (foundReasoningField) {
                  if (!currentBlock || currentBlock.type !== "thinking") {
                    finishCurrentBlock(currentBlock);
                    currentBlock = {
                      type: "thinking",
                      thinking: "",
                      thinkingSignature: foundReasoningField
                    };
                    output.content.push(currentBlock);
                    stream.push({
                      type: "thinking_start",
                      contentIndex: blockIndex(),
                      partial: output
                    });
                  }
                  if (currentBlock.type === "thinking") {
                    const delta = choice.delta[foundReasoningField];
                    currentBlock.thinking += delta;
                    stream.push({
                      type: "thinking_delta",
                      contentIndex: blockIndex(),
                      delta,
                      partial: output
                    });
                  }
                }
                if (choice.delta.tool_calls) {
                  for (const toolCall of choice.delta.tool_calls) {
                    if (!currentBlock || currentBlock.type !== "toolCall" || toolCall.id && currentBlock.id !== toolCall.id) {
                      finishCurrentBlock(currentBlock);
                      currentBlock = {
                        type: "toolCall",
                        id: toolCall.id || "",
                        name: toolCall.function?.name || "",
                        arguments: {},
                        partialArgs: ""
                      };
                      output.content.push(currentBlock);
                      stream.push({
                        type: "toolcall_start",
                        contentIndex: blockIndex(),
                        partial: output
                      });
                    }
                    if (currentBlock.type === "toolCall") {
                      if (toolCall.id)
                        currentBlock.id = toolCall.id;
                      if (toolCall.function?.name)
                        currentBlock.name = toolCall.function.name;
                      let delta = "";
                      if (toolCall.function?.arguments) {
                        delta = toolCall.function.arguments;
                        currentBlock.partialArgs += toolCall.function.arguments;
                        currentBlock.arguments = parseStreamingJson(currentBlock.partialArgs);
                      }
                      stream.push({
                        type: "toolcall_delta",
                        contentIndex: blockIndex(),
                        delta,
                        partial: output
                      });
                    }
                  }
                }
              }
            }
          }
        }
        finishCurrentBlock(currentBlock);
        if (options?.signal?.aborted) {
          throw new Error("Request was aborted");
        }
        if (output.stopReason === "aborted" || output.stopReason === "error") {
          throw new Error("An unknown error occurred");
        }
        stream.push({ type: "done", reason: output.stopReason, message: output });
        stream.end();
      } catch (error) {
        output.stopReason = options?.signal?.aborted ? "aborted" : "error";
        output.errorMessage = error instanceof Error ? error.message : JSON.stringify(error);
        stream.push({ type: "error", reason: output.stopReason, error: output });
        stream.end();
      }
    })();
    return stream;
  };
  var streamSimpleOpenAICompletions = (model, context, options) => {
    const apiKey = options?.apiKey || getEnvApiKey(model.provider);
    if (!apiKey) {
      throw new Error(`No API key for provider: ${model.provider}`);
    }
    const base = buildBaseOptions(model, options, apiKey);
    const reasoningEffort = clampReasoning(options?.reasoning);
    return streamOpenAICompletions(model, context, {
      ...base,
      reasoningEffort
    });
  };
  function buildParams(model, context, options, compat) {
    const messages = convertMessages(model, context, compat);
    const params = {
      model: model.id,
      messages,
      stream: true
    };
    if (compat.supportsUsageInStreaming !== false) {
      params.stream_options = { include_usage: true };
    }
    if (compat.supportsStore) {
      params.store = options?.cacheRetention !== "none";
    }
    if (options?.sessionId && compat.supportsStore) {
      params.session_id = options.sessionId;
    }
    if (process.env.PIE_DEBUG_CACHE) {
      console.error("[CACHE DEBUG] Request params:", JSON.stringify({
        model: params.model,
        store: params.store,
        session_id: params.session_id,
        cacheRetention: options?.cacheRetention,
        supportsStore: compat.supportsStore,
        provider: model.provider
      }, null, 2));
    }
    if (options?.maxTokens) {
      if (compat.maxTokensField === "max_tokens") {
        params.max_tokens = options.maxTokens;
      } else {
        params.max_completion_tokens = options.maxTokens;
      }
    }
    if (options?.temperature !== void 0) {
      params.temperature = options.temperature;
    }
    if (context.tools) {
      params.tools = convertTools(context.tools, compat);
    }
    if (options?.toolChoice) {
      params.tool_choice = options.toolChoice;
    }
    if (options?.reasoningEffort && model.reasoning && compat.supportsReasoningEffort) {
      params.reasoning_effort = options.reasoningEffort;
    }
    return params;
  }
  function convertMessages(model, context, compat) {
    const params = [];
    const normalizeToolCallId2 = (id) => {
      return id.length > 40 ? id.slice(0, 40) : id;
    };
    const transformedMessages = transformMessages(context.messages, model, (id) => normalizeToolCallId2(id));
    if (context.systemPrompt) {
      const useDeveloperRole = model.reasoning && compat.supportsDeveloperRole;
      const role = useDeveloperRole ? "developer" : "system";
      params.push({ role, content: context.systemPrompt });
    }
    for (let i = 0; i < transformedMessages.length; i++) {
      const msg = transformedMessages[i];
      if (msg.role === "user") {
        if (typeof msg.content === "string") {
          params.push({ role: "user", content: msg.content });
        } else {
          const content = msg.content.flatMap((item) => convertUserContentItem(item));
          const filteredContent = content.filter((c) => {
            if (c.type === "text")
              return true;
            if (c.type === "image_url")
              return model.input.includes("image");
            if (c.type === "video_url")
              return model.input.includes("video");
            return true;
          });
          if (filteredContent.length === 0)
            continue;
          params.push({ role: "user", content: filteredContent });
        }
      } else if (msg.role === "assistant") {
        const assistantMsg = {
          role: "assistant",
          content: null
        };
        const textBlocks = msg.content.filter((b) => b.type === "text");
        const nonEmptyTextBlocks = textBlocks.filter((b) => b.text && b.text.trim().length > 0);
        if (nonEmptyTextBlocks.length > 0) {
          assistantMsg.content = nonEmptyTextBlocks.map((b) => ({
            type: "text",
            text: b.text
          }));
        }
        const thinkingBlocks = msg.content.filter((b) => b.type === "thinking");
        const nonEmptyThinkingBlocks = thinkingBlocks.filter((b) => b.thinking && b.thinking.trim().length > 0);
        if (nonEmptyThinkingBlocks.length > 0) {
          if (compat.requiresThinkingAsText) {
            const thinkingText = nonEmptyThinkingBlocks.map((b) => b.thinking).join("\n\n");
            const textContent = assistantMsg.content;
            if (textContent) {
              textContent.unshift({ type: "text", text: thinkingText });
            } else {
              assistantMsg.content = [{ type: "text", text: thinkingText }];
            }
          } else {
            const signature = nonEmptyThinkingBlocks[0].thinkingSignature;
            if (signature && signature.length > 0) {
              assistantMsg[signature] = nonEmptyThinkingBlocks.map((b) => b.thinking).join("\n");
            }
          }
        }
        const toolCalls = msg.content.filter((b) => b.type === "toolCall");
        if (toolCalls.length > 0) {
          assistantMsg.tool_calls = toolCalls.map((tc) => ({
            id: tc.id,
            type: "function",
            function: {
              name: tc.name,
              arguments: JSON.stringify(tc.arguments)
            }
          }));
          if (!compat.requiresThinkingAsText && !assistantMsg.reasoning_content) {
            assistantMsg.reasoning_content = "";
          }
        }
        const content = assistantMsg.content;
        const hasContent = content !== null && content !== void 0 && (typeof content === "string" ? content.length > 0 : content.length > 0);
        if (!hasContent && !assistantMsg.tool_calls) {
          continue;
        }
        params.push(assistantMsg);
      } else if (msg.role === "toolResult") {
        for (; i < transformedMessages.length && transformedMessages[i].role === "toolResult"; i++) {
          const toolMsg = transformedMessages[i];
          const textResult = toolMsg.content.filter((c) => c.type === "text").map((c) => c.text).join("\n");
          const toolResultMsg = {
            role: "tool",
            content: textResult || "(empty result)",
            tool_call_id: toolMsg.toolCallId
          };
          if (compat.requiresToolResultName && toolMsg.toolName) {
            toolResultMsg.name = toolMsg.toolName;
          }
          params.push(toolResultMsg);
        }
        i--;
      }
    }
    return params;
  }
  function convertUserContentItem(item) {
    if (item.type === "text") {
      return [{ type: "text", text: item.text }];
    }
    if (item.type === "image") {
      return [{
        type: "image_url",
        image_url: { url: `data:${item.mimeType};base64,${item.data}` }
      }];
    }
    if (item.type === "video") {
      const video = item;
      return [{
        type: "video_url",
        video_url: { url: `data:${video.mimeType};base64,${video.data}` }
      }];
    }
    if (item.type === "fileRef") {
      const ref = item;
      if (ref.modality === "image") {
        return [{ type: "image_url", image_url: { url: ref.url } }];
      }
      if (ref.modality === "video") {
        return [{ type: "video_url", video_url: { url: ref.url } }];
      }
      return [{ type: "file", file_url: { url: ref.url } }];
    }
    return [];
  }
  function convertTools(tools, compat) {
    return tools.map((tool) => ({
      type: "function",
      function: {
        name: tool.name,
        description: tool.description,
        parameters: tool.parameters,
        ...compat.supportsStrictMode !== false && { strict: false }
      }
    }));
  }
  function mapStopReason(reason) {
    if (reason === null)
      return "stop";
    switch (reason) {
      case "stop":
        return "stop";
      case "length":
        return "length";
      case "function_call":
      case "tool_calls":
        return "toolUse";
      case "content_filter":
        return "error";
      default:
        return "stop";
    }
  }
  function detectCompat(model) {
    const isMoonshot = model.provider === "kimi-cn" || model.baseUrl.includes("moonshot.cn") || model.baseUrl.includes("moonshot.ai");
    return {
      // Moonshot/Kimi supports conversation storage for context caching
      supportsStore: isMoonshot,
      supportsDeveloperRole: false,
      supportsReasoningEffort: false,
      supportsUsageInStreaming: true,
      maxTokensField: "max_tokens",
      requiresToolResultName: false,
      requiresAssistantAfterToolResult: false,
      // Kimi K2.5 returns reasoning_content and requires it replayed as-is
      requiresThinkingAsText: !isMoonshot,
      supportsStrictMode: true
    };
  }
  function getCompat(model) {
    const detected = detectCompat(model);
    if (!model.compat)
      return detected;
    return {
      supportsStore: model.compat.supportsStore ?? detected.supportsStore,
      supportsDeveloperRole: model.compat.supportsDeveloperRole ?? detected.supportsDeveloperRole,
      supportsReasoningEffort: model.compat.supportsReasoningEffort ?? detected.supportsReasoningEffort,
      supportsUsageInStreaming: model.compat.supportsUsageInStreaming ?? detected.supportsUsageInStreaming,
      maxTokensField: model.compat.maxTokensField ?? detected.maxTokensField,
      requiresToolResultName: model.compat.requiresToolResultName ?? detected.requiresToolResultName,
      requiresAssistantAfterToolResult: model.compat.requiresAssistantAfterToolResult ?? detected.requiresAssistantAfterToolResult,
      requiresThinkingAsText: model.compat.requiresThinkingAsText ?? detected.requiresThinkingAsText,
      supportsStrictMode: model.compat.supportsStrictMode ?? detected.supportsStrictMode
    };
  }

  // ../../node_modules/@anthropic-ai/sdk/version.mjs
  var VERSION = "0.39.0";

  // ../../node_modules/@anthropic-ai/sdk/_shims/registry.mjs
  var auto = false;
  var kind = void 0;
  var fetch2 = void 0;
  var Request2 = void 0;
  var Response2 = void 0;
  var Headers2 = void 0;
  var FormData2 = void 0;
  var Blob2 = void 0;
  var File2 = void 0;
  var ReadableStream2 = void 0;
  var getMultipartRequestOptions = void 0;
  var getDefaultAgent = void 0;
  var fileFromPath = void 0;
  var isFsReadStream = void 0;
  function setShims(shims, options = { auto: false }) {
    if (auto) {
      throw new Error(`you must \`import '@anthropic-ai/sdk/shims/${shims.kind}'\` before importing anything else from @anthropic-ai/sdk`);
    }
    if (kind) {
      throw new Error(`can't \`import '@anthropic-ai/sdk/shims/${shims.kind}'\` after \`import '@anthropic-ai/sdk/shims/${kind}'\``);
    }
    auto = options.auto;
    kind = shims.kind;
    fetch2 = shims.fetch;
    Request2 = shims.Request;
    Response2 = shims.Response;
    Headers2 = shims.Headers;
    FormData2 = shims.FormData;
    Blob2 = shims.Blob;
    File2 = shims.File;
    ReadableStream2 = shims.ReadableStream;
    getMultipartRequestOptions = shims.getMultipartRequestOptions;
    getDefaultAgent = shims.getDefaultAgent;
    fileFromPath = shims.fileFromPath;
    isFsReadStream = shims.isFsReadStream;
  }

  // ../../node_modules/@anthropic-ai/sdk/_shims/MultipartBody.mjs
  var MultipartBody = class {
    constructor(body) {
      this.body = body;
    }
    get [Symbol.toStringTag]() {
      return "MultipartBody";
    }
  };

  // ../../node_modules/@anthropic-ai/sdk/_shims/web-runtime.mjs
  function getRuntime({ manuallyImported } = {}) {
    const recommendation = manuallyImported ? `You may need to use polyfills` : `Add one of these imports before your first \`import \u2026 from '@anthropic-ai/sdk'\`:
- \`import '@anthropic-ai/sdk/shims/node'\` (if you're running on Node)
- \`import '@anthropic-ai/sdk/shims/web'\` (otherwise)
`;
    let _fetch, _Request, _Response, _Headers;
    try {
      _fetch = fetch;
      _Request = Request;
      _Response = Response;
      _Headers = Headers;
    } catch (error) {
      throw new Error(`this environment is missing the following Web Fetch API type: ${error.message}. ${recommendation}`);
    }
    return {
      kind: "web",
      fetch: _fetch,
      Request: _Request,
      Response: _Response,
      Headers: _Headers,
      FormData: (
        // @ts-ignore
        typeof FormData !== "undefined" ? FormData : class FormData {
          // @ts-ignore
          constructor() {
            throw new Error(`file uploads aren't supported in this environment yet as 'FormData' is undefined. ${recommendation}`);
          }
        }
      ),
      Blob: typeof Blob !== "undefined" ? Blob : class Blob {
        constructor() {
          throw new Error(`file uploads aren't supported in this environment yet as 'Blob' is undefined. ${recommendation}`);
        }
      },
      File: (
        // @ts-ignore
        typeof File !== "undefined" ? File : class File {
          // @ts-ignore
          constructor() {
            throw new Error(`file uploads aren't supported in this environment yet as 'File' is undefined. ${recommendation}`);
          }
        }
      ),
      ReadableStream: (
        // @ts-ignore
        typeof ReadableStream !== "undefined" ? ReadableStream : class ReadableStream {
          // @ts-ignore
          constructor() {
            throw new Error(`streaming isn't supported in this environment yet as 'ReadableStream' is undefined. ${recommendation}`);
          }
        }
      ),
      getMultipartRequestOptions: async (form, opts) => ({
        ...opts,
        body: new MultipartBody(form)
      }),
      getDefaultAgent: (url) => void 0,
      fileFromPath: () => {
        throw new Error("The `fileFromPath` function is only supported in Node. See the README for more details: https://www.github.com/anthropics/anthropic-sdk-typescript#file-uploads");
      },
      isFsReadStream: (value) => false
    };
  }

  // ../../node_modules/@anthropic-ai/sdk/_shims/index.mjs
  if (!kind) setShims(getRuntime(), { auto: true });

  // ../../node_modules/@anthropic-ai/sdk/error.mjs
  var AnthropicError = class extends Error {
  };
  var APIError = class _APIError extends AnthropicError {
    constructor(status, error, message, headers) {
      super(`${_APIError.makeMessage(status, error, message)}`);
      this.status = status;
      this.headers = headers;
      this.request_id = headers?.["request-id"];
      this.error = error;
    }
    static makeMessage(status, error, message) {
      const msg = error?.message ? typeof error.message === "string" ? error.message : JSON.stringify(error.message) : error ? JSON.stringify(error) : message;
      if (status && msg) {
        return `${status} ${msg}`;
      }
      if (status) {
        return `${status} status code (no body)`;
      }
      if (msg) {
        return msg;
      }
      return "(no status code or body)";
    }
    static generate(status, errorResponse, message, headers) {
      if (!status || !headers) {
        return new APIConnectionError({ message, cause: castToError(errorResponse) });
      }
      const error = errorResponse;
      if (status === 400) {
        return new BadRequestError(status, error, message, headers);
      }
      if (status === 401) {
        return new AuthenticationError(status, error, message, headers);
      }
      if (status === 403) {
        return new PermissionDeniedError(status, error, message, headers);
      }
      if (status === 404) {
        return new NotFoundError(status, error, message, headers);
      }
      if (status === 409) {
        return new ConflictError(status, error, message, headers);
      }
      if (status === 422) {
        return new UnprocessableEntityError(status, error, message, headers);
      }
      if (status === 429) {
        return new RateLimitError(status, error, message, headers);
      }
      if (status >= 500) {
        return new InternalServerError(status, error, message, headers);
      }
      return new _APIError(status, error, message, headers);
    }
  };
  var APIUserAbortError = class extends APIError {
    constructor({ message } = {}) {
      super(void 0, void 0, message || "Request was aborted.", void 0);
    }
  };
  var APIConnectionError = class extends APIError {
    constructor({ message, cause }) {
      super(void 0, void 0, message || "Connection error.", void 0);
      if (cause)
        this.cause = cause;
    }
  };
  var APIConnectionTimeoutError = class extends APIConnectionError {
    constructor({ message } = {}) {
      super({ message: message ?? "Request timed out." });
    }
  };
  var BadRequestError = class extends APIError {
  };
  var AuthenticationError = class extends APIError {
  };
  var PermissionDeniedError = class extends APIError {
  };
  var NotFoundError = class extends APIError {
  };
  var ConflictError = class extends APIError {
  };
  var UnprocessableEntityError = class extends APIError {
  };
  var RateLimitError = class extends APIError {
  };
  var InternalServerError = class extends APIError {
  };

  // ../../node_modules/@anthropic-ai/sdk/internal/decoders/line.mjs
  var __classPrivateFieldSet = function(receiver, state, value, kind2, f) {
    if (kind2 === "m") throw new TypeError("Private method is not writable");
    if (kind2 === "a" && !f) throw new TypeError("Private accessor was defined without a setter");
    if (typeof state === "function" ? receiver !== state || !f : !state.has(receiver)) throw new TypeError("Cannot write private member to an object whose class did not declare it");
    return kind2 === "a" ? f.call(receiver, value) : f ? f.value = value : state.set(receiver, value), value;
  };
  var __classPrivateFieldGet = function(receiver, state, kind2, f) {
    if (kind2 === "a" && !f) throw new TypeError("Private accessor was defined without a getter");
    if (typeof state === "function" ? receiver !== state || !f : !state.has(receiver)) throw new TypeError("Cannot read private member from an object whose class did not declare it");
    return kind2 === "m" ? f : kind2 === "a" ? f.call(receiver) : f ? f.value : state.get(receiver);
  };
  var _LineDecoder_carriageReturnIndex;
  var LineDecoder = class {
    constructor() {
      _LineDecoder_carriageReturnIndex.set(this, void 0);
      this.buffer = new Uint8Array();
      __classPrivateFieldSet(this, _LineDecoder_carriageReturnIndex, null, "f");
    }
    decode(chunk) {
      if (chunk == null) {
        return [];
      }
      const binaryChunk = chunk instanceof ArrayBuffer ? new Uint8Array(chunk) : typeof chunk === "string" ? new TextEncoder().encode(chunk) : chunk;
      let newData = new Uint8Array(this.buffer.length + binaryChunk.length);
      newData.set(this.buffer);
      newData.set(binaryChunk, this.buffer.length);
      this.buffer = newData;
      const lines = [];
      let patternIndex;
      while ((patternIndex = findNewlineIndex(this.buffer, __classPrivateFieldGet(this, _LineDecoder_carriageReturnIndex, "f"))) != null) {
        if (patternIndex.carriage && __classPrivateFieldGet(this, _LineDecoder_carriageReturnIndex, "f") == null) {
          __classPrivateFieldSet(this, _LineDecoder_carriageReturnIndex, patternIndex.index, "f");
          continue;
        }
        if (__classPrivateFieldGet(this, _LineDecoder_carriageReturnIndex, "f") != null && (patternIndex.index !== __classPrivateFieldGet(this, _LineDecoder_carriageReturnIndex, "f") + 1 || patternIndex.carriage)) {
          lines.push(this.decodeText(this.buffer.slice(0, __classPrivateFieldGet(this, _LineDecoder_carriageReturnIndex, "f") - 1)));
          this.buffer = this.buffer.slice(__classPrivateFieldGet(this, _LineDecoder_carriageReturnIndex, "f"));
          __classPrivateFieldSet(this, _LineDecoder_carriageReturnIndex, null, "f");
          continue;
        }
        const endIndex = __classPrivateFieldGet(this, _LineDecoder_carriageReturnIndex, "f") !== null ? patternIndex.preceding - 1 : patternIndex.preceding;
        const line = this.decodeText(this.buffer.slice(0, endIndex));
        lines.push(line);
        this.buffer = this.buffer.slice(patternIndex.index);
        __classPrivateFieldSet(this, _LineDecoder_carriageReturnIndex, null, "f");
      }
      return lines;
    }
    decodeText(bytes) {
      if (bytes == null)
        return "";
      if (typeof bytes === "string")
        return bytes;
      if (typeof Buffer !== "undefined") {
        if (bytes instanceof Buffer) {
          return bytes.toString();
        }
        if (bytes instanceof Uint8Array) {
          return Buffer.from(bytes).toString();
        }
        throw new AnthropicError(`Unexpected: received non-Uint8Array (${bytes.constructor.name}) stream chunk in an environment with a global "Buffer" defined, which this library assumes to be Node. Please report this error.`);
      }
      if (typeof TextDecoder !== "undefined") {
        if (bytes instanceof Uint8Array || bytes instanceof ArrayBuffer) {
          this.textDecoder ?? (this.textDecoder = new TextDecoder("utf8"));
          return this.textDecoder.decode(bytes);
        }
        throw new AnthropicError(`Unexpected: received non-Uint8Array/ArrayBuffer (${bytes.constructor.name}) in a web platform. Please report this error.`);
      }
      throw new AnthropicError(`Unexpected: neither Buffer nor TextDecoder are available as globals. Please report this error.`);
    }
    flush() {
      if (!this.buffer.length) {
        return [];
      }
      return this.decode("\n");
    }
  };
  _LineDecoder_carriageReturnIndex = /* @__PURE__ */ new WeakMap();
  LineDecoder.NEWLINE_CHARS = /* @__PURE__ */ new Set(["\n", "\r"]);
  LineDecoder.NEWLINE_REGEXP = /\r\n|[\n\r]/g;
  function findNewlineIndex(buffer, startIndex) {
    const newline = 10;
    const carriage = 13;
    for (let i = startIndex ?? 0; i < buffer.length; i++) {
      if (buffer[i] === newline) {
        return { preceding: i, index: i + 1, carriage: false };
      }
      if (buffer[i] === carriage) {
        return { preceding: i, index: i + 1, carriage: true };
      }
    }
    return null;
  }
  function findDoubleNewlineIndex(buffer) {
    const newline = 10;
    const carriage = 13;
    for (let i = 0; i < buffer.length - 1; i++) {
      if (buffer[i] === newline && buffer[i + 1] === newline) {
        return i + 2;
      }
      if (buffer[i] === carriage && buffer[i + 1] === carriage) {
        return i + 2;
      }
      if (buffer[i] === carriage && buffer[i + 1] === newline && i + 3 < buffer.length && buffer[i + 2] === carriage && buffer[i + 3] === newline) {
        return i + 4;
      }
    }
    return -1;
  }

  // ../../node_modules/@anthropic-ai/sdk/internal/stream-utils.mjs
  function ReadableStreamToAsyncIterable(stream) {
    if (stream[Symbol.asyncIterator])
      return stream;
    const reader = stream.getReader();
    return {
      async next() {
        try {
          const result = await reader.read();
          if (result?.done)
            reader.releaseLock();
          return result;
        } catch (e) {
          reader.releaseLock();
          throw e;
        }
      },
      async return() {
        const cancelPromise = reader.cancel();
        reader.releaseLock();
        await cancelPromise;
        return { done: true, value: void 0 };
      },
      [Symbol.asyncIterator]() {
        return this;
      }
    };
  }

  // ../../node_modules/@anthropic-ai/sdk/streaming.mjs
  var Stream = class _Stream {
    constructor(iterator, controller) {
      this.iterator = iterator;
      this.controller = controller;
    }
    static fromSSEResponse(response, controller) {
      let consumed = false;
      async function* iterator() {
        if (consumed) {
          throw new Error("Cannot iterate over a consumed stream, use `.tee()` to split the stream.");
        }
        consumed = true;
        let done = false;
        try {
          for await (const sse of _iterSSEMessages(response, controller)) {
            if (sse.event === "completion") {
              try {
                yield JSON.parse(sse.data);
              } catch (e) {
                console.error(`Could not parse message into JSON:`, sse.data);
                console.error(`From chunk:`, sse.raw);
                throw e;
              }
            }
            if (sse.event === "message_start" || sse.event === "message_delta" || sse.event === "message_stop" || sse.event === "content_block_start" || sse.event === "content_block_delta" || sse.event === "content_block_stop") {
              try {
                yield JSON.parse(sse.data);
              } catch (e) {
                console.error(`Could not parse message into JSON:`, sse.data);
                console.error(`From chunk:`, sse.raw);
                throw e;
              }
            }
            if (sse.event === "ping") {
              continue;
            }
            if (sse.event === "error") {
              throw APIError.generate(void 0, `SSE Error: ${sse.data}`, sse.data, createResponseHeaders(response.headers));
            }
          }
          done = true;
        } catch (e) {
          if (e instanceof Error && e.name === "AbortError")
            return;
          throw e;
        } finally {
          if (!done)
            controller.abort();
        }
      }
      return new _Stream(iterator, controller);
    }
    /**
     * Generates a Stream from a newline-separated ReadableStream
     * where each item is a JSON value.
     */
    static fromReadableStream(readableStream, controller) {
      let consumed = false;
      async function* iterLines() {
        const lineDecoder = new LineDecoder();
        const iter = ReadableStreamToAsyncIterable(readableStream);
        for await (const chunk of iter) {
          for (const line of lineDecoder.decode(chunk)) {
            yield line;
          }
        }
        for (const line of lineDecoder.flush()) {
          yield line;
        }
      }
      async function* iterator() {
        if (consumed) {
          throw new Error("Cannot iterate over a consumed stream, use `.tee()` to split the stream.");
        }
        consumed = true;
        let done = false;
        try {
          for await (const line of iterLines()) {
            if (done)
              continue;
            if (line)
              yield JSON.parse(line);
          }
          done = true;
        } catch (e) {
          if (e instanceof Error && e.name === "AbortError")
            return;
          throw e;
        } finally {
          if (!done)
            controller.abort();
        }
      }
      return new _Stream(iterator, controller);
    }
    [Symbol.asyncIterator]() {
      return this.iterator();
    }
    /**
     * Splits the stream into two streams which can be
     * independently read from at different speeds.
     */
    tee() {
      const left = [];
      const right = [];
      const iterator = this.iterator();
      const teeIterator = (queue) => {
        return {
          next: () => {
            if (queue.length === 0) {
              const result = iterator.next();
              left.push(result);
              right.push(result);
            }
            return queue.shift();
          }
        };
      };
      return [
        new _Stream(() => teeIterator(left), this.controller),
        new _Stream(() => teeIterator(right), this.controller)
      ];
    }
    /**
     * Converts this stream to a newline-separated ReadableStream of
     * JSON stringified values in the stream
     * which can be turned back into a Stream with `Stream.fromReadableStream()`.
     */
    toReadableStream() {
      const self = this;
      let iter;
      const encoder = new TextEncoder();
      return new ReadableStream2({
        async start() {
          iter = self[Symbol.asyncIterator]();
        },
        async pull(ctrl) {
          try {
            const { value, done } = await iter.next();
            if (done)
              return ctrl.close();
            const bytes = encoder.encode(JSON.stringify(value) + "\n");
            ctrl.enqueue(bytes);
          } catch (err) {
            ctrl.error(err);
          }
        },
        async cancel() {
          await iter.return?.();
        }
      });
    }
  };
  async function* _iterSSEMessages(response, controller) {
    if (!response.body) {
      controller.abort();
      throw new AnthropicError(`Attempted to iterate over a response with no body`);
    }
    const sseDecoder = new SSEDecoder();
    const lineDecoder = new LineDecoder();
    const iter = ReadableStreamToAsyncIterable(response.body);
    for await (const sseChunk of iterSSEChunks(iter)) {
      for (const line of lineDecoder.decode(sseChunk)) {
        const sse = sseDecoder.decode(line);
        if (sse)
          yield sse;
      }
    }
    for (const line of lineDecoder.flush()) {
      const sse = sseDecoder.decode(line);
      if (sse)
        yield sse;
    }
  }
  async function* iterSSEChunks(iterator) {
    let data = new Uint8Array();
    for await (const chunk of iterator) {
      if (chunk == null) {
        continue;
      }
      const binaryChunk = chunk instanceof ArrayBuffer ? new Uint8Array(chunk) : typeof chunk === "string" ? new TextEncoder().encode(chunk) : chunk;
      let newData = new Uint8Array(data.length + binaryChunk.length);
      newData.set(data);
      newData.set(binaryChunk, data.length);
      data = newData;
      let patternIndex;
      while ((patternIndex = findDoubleNewlineIndex(data)) !== -1) {
        yield data.slice(0, patternIndex);
        data = data.slice(patternIndex);
      }
    }
    if (data.length > 0) {
      yield data;
    }
  }
  var SSEDecoder = class {
    constructor() {
      this.event = null;
      this.data = [];
      this.chunks = [];
    }
    decode(line) {
      if (line.endsWith("\r")) {
        line = line.substring(0, line.length - 1);
      }
      if (!line) {
        if (!this.event && !this.data.length)
          return null;
        const sse = {
          event: this.event,
          data: this.data.join("\n"),
          raw: this.chunks
        };
        this.event = null;
        this.data = [];
        this.chunks = [];
        return sse;
      }
      this.chunks.push(line);
      if (line.startsWith(":")) {
        return null;
      }
      let [fieldname, _, value] = partition(line, ":");
      if (value.startsWith(" ")) {
        value = value.substring(1);
      }
      if (fieldname === "event") {
        this.event = value;
      } else if (fieldname === "data") {
        this.data.push(value);
      }
      return null;
    }
  };
  function partition(str, delimiter) {
    const index = str.indexOf(delimiter);
    if (index !== -1) {
      return [str.substring(0, index), delimiter, str.substring(index + delimiter.length)];
    }
    return [str, "", ""];
  }

  // ../../node_modules/@anthropic-ai/sdk/uploads.mjs
  var isResponseLike = (value) => value != null && typeof value === "object" && typeof value.url === "string" && typeof value.blob === "function";
  var isFileLike = (value) => value != null && typeof value === "object" && typeof value.name === "string" && typeof value.lastModified === "number" && isBlobLike(value);
  var isBlobLike = (value) => value != null && typeof value === "object" && typeof value.size === "number" && typeof value.type === "string" && typeof value.text === "function" && typeof value.slice === "function" && typeof value.arrayBuffer === "function";
  async function toFile(value, name, options) {
    value = await value;
    if (isFileLike(value)) {
      return value;
    }
    if (isResponseLike(value)) {
      const blob = await value.blob();
      name || (name = new URL(value.url).pathname.split(/[\\/]/).pop() ?? "unknown_file");
      const data = isBlobLike(blob) ? [await blob.arrayBuffer()] : [blob];
      return new File2(data, name, options);
    }
    const bits = await getBytes(value);
    name || (name = getName(value) ?? "unknown_file");
    if (!options?.type) {
      const type = bits[0]?.type;
      if (typeof type === "string") {
        options = { ...options, type };
      }
    }
    return new File2(bits, name, options);
  }
  async function getBytes(value) {
    let parts = [];
    if (typeof value === "string" || ArrayBuffer.isView(value) || // includes Uint8Array, Buffer, etc.
    value instanceof ArrayBuffer) {
      parts.push(value);
    } else if (isBlobLike(value)) {
      parts.push(await value.arrayBuffer());
    } else if (isAsyncIterableIterator(value)) {
      for await (const chunk of value) {
        parts.push(chunk);
      }
    } else {
      throw new Error(`Unexpected data type: ${typeof value}; constructor: ${value?.constructor?.name}; props: ${propsForError(value)}`);
    }
    return parts;
  }
  function propsForError(value) {
    const props = Object.getOwnPropertyNames(value);
    return `[${props.map((p) => `"${p}"`).join(", ")}]`;
  }
  function getName(value) {
    return getStringFromMaybeBuffer(value.name) || getStringFromMaybeBuffer(value.filename) || // For fs.ReadStream
    getStringFromMaybeBuffer(value.path)?.split(/[\\/]/).pop();
  }
  var getStringFromMaybeBuffer = (x) => {
    if (typeof x === "string")
      return x;
    if (typeof Buffer !== "undefined" && x instanceof Buffer)
      return String(x);
    return void 0;
  };
  var isAsyncIterableIterator = (value) => value != null && typeof value === "object" && typeof value[Symbol.asyncIterator] === "function";
  var isMultipartBody = (body) => body && typeof body === "object" && body.body && body[Symbol.toStringTag] === "MultipartBody";

  // ../../node_modules/@anthropic-ai/sdk/core.mjs
  var __classPrivateFieldSet2 = function(receiver, state, value, kind2, f) {
    if (kind2 === "m") throw new TypeError("Private method is not writable");
    if (kind2 === "a" && !f) throw new TypeError("Private accessor was defined without a setter");
    if (typeof state === "function" ? receiver !== state || !f : !state.has(receiver)) throw new TypeError("Cannot write private member to an object whose class did not declare it");
    return kind2 === "a" ? f.call(receiver, value) : f ? f.value = value : state.set(receiver, value), value;
  };
  var __classPrivateFieldGet2 = function(receiver, state, kind2, f) {
    if (kind2 === "a" && !f) throw new TypeError("Private accessor was defined without a getter");
    if (typeof state === "function" ? receiver !== state || !f : !state.has(receiver)) throw new TypeError("Cannot read private member from an object whose class did not declare it");
    return kind2 === "m" ? f : kind2 === "a" ? f.call(receiver) : f ? f.value : state.get(receiver);
  };
  var _AbstractPage_client;
  async function defaultParseResponse(props) {
    const { response } = props;
    if (props.options.stream) {
      debug("response", response.status, response.url, response.headers, response.body);
      if (props.options.__streamClass) {
        return props.options.__streamClass.fromSSEResponse(response, props.controller);
      }
      return Stream.fromSSEResponse(response, props.controller);
    }
    if (response.status === 204) {
      return null;
    }
    if (props.options.__binaryResponse) {
      return response;
    }
    const contentType = response.headers.get("content-type");
    const isJSON = contentType?.includes("application/json") || contentType?.includes("application/vnd.api+json");
    if (isJSON) {
      const json = await response.json();
      debug("response", response.status, response.url, response.headers, json);
      return _addRequestID(json, response);
    }
    const text = await response.text();
    debug("response", response.status, response.url, response.headers, text);
    return text;
  }
  function _addRequestID(value, response) {
    if (!value || typeof value !== "object" || Array.isArray(value)) {
      return value;
    }
    return Object.defineProperty(value, "_request_id", {
      value: response.headers.get("request-id"),
      enumerable: false
    });
  }
  var APIPromise = class _APIPromise extends Promise {
    constructor(responsePromise, parseResponse = defaultParseResponse) {
      super((resolve) => {
        resolve(null);
      });
      this.responsePromise = responsePromise;
      this.parseResponse = parseResponse;
    }
    _thenUnwrap(transform) {
      return new _APIPromise(this.responsePromise, async (props) => _addRequestID(transform(await this.parseResponse(props), props), props.response));
    }
    /**
     * Gets the raw `Response` instance instead of parsing the response
     * data.
     *
     * If you want to parse the response body but still get the `Response`
     * instance, you can use {@link withResponse()}.
     *
     * 👋 Getting the wrong TypeScript type for `Response`?
     * Try setting `"moduleResolution": "NodeNext"` if you can,
     * or add one of these imports before your first `import … from '@anthropic-ai/sdk'`:
     * - `import '@anthropic-ai/sdk/shims/node'` (if you're running on Node)
     * - `import '@anthropic-ai/sdk/shims/web'` (otherwise)
     */
    asResponse() {
      return this.responsePromise.then((p) => p.response);
    }
    /**
     * Gets the parsed response data, the raw `Response` instance and the ID of the request,
     * returned vie the `request-id` header which is useful for debugging requests and resporting
     * issues to Anthropic.
     *
     * If you just want to get the raw `Response` instance without parsing it,
     * you can use {@link asResponse()}.
     *
     * 👋 Getting the wrong TypeScript type for `Response`?
     * Try setting `"moduleResolution": "NodeNext"` if you can,
     * or add one of these imports before your first `import … from '@anthropic-ai/sdk'`:
     * - `import '@anthropic-ai/sdk/shims/node'` (if you're running on Node)
     * - `import '@anthropic-ai/sdk/shims/web'` (otherwise)
     */
    async withResponse() {
      const [data, response] = await Promise.all([this.parse(), this.asResponse()]);
      return { data, response, request_id: response.headers.get("request-id") };
    }
    parse() {
      if (!this.parsedPromise) {
        this.parsedPromise = this.responsePromise.then(this.parseResponse);
      }
      return this.parsedPromise;
    }
    then(onfulfilled, onrejected) {
      return this.parse().then(onfulfilled, onrejected);
    }
    catch(onrejected) {
      return this.parse().catch(onrejected);
    }
    finally(onfinally) {
      return this.parse().finally(onfinally);
    }
  };
  var APIClient = class {
    constructor({
      baseURL,
      maxRetries = 2,
      timeout = 6e5,
      // 10 minutes
      httpAgent,
      fetch: overriddenFetch
    }) {
      this.baseURL = baseURL;
      this.maxRetries = validatePositiveInteger("maxRetries", maxRetries);
      this.timeout = validatePositiveInteger("timeout", timeout);
      this.httpAgent = httpAgent;
      this.fetch = overriddenFetch ?? fetch2;
    }
    authHeaders(opts) {
      return {};
    }
    /**
     * Override this to add your own default headers, for example:
     *
     *  {
     *    ...super.defaultHeaders(),
     *    Authorization: 'Bearer 123',
     *  }
     */
    defaultHeaders(opts) {
      return {
        Accept: "application/json",
        "Content-Type": "application/json",
        "User-Agent": this.getUserAgent(),
        ...getPlatformHeaders(),
        ...this.authHeaders(opts)
      };
    }
    /**
     * Override this to add your own headers validation:
     */
    validateHeaders(headers, customHeaders) {
    }
    defaultIdempotencyKey() {
      return `stainless-node-retry-${uuid4()}`;
    }
    get(path, opts) {
      return this.methodRequest("get", path, opts);
    }
    post(path, opts) {
      return this.methodRequest("post", path, opts);
    }
    patch(path, opts) {
      return this.methodRequest("patch", path, opts);
    }
    put(path, opts) {
      return this.methodRequest("put", path, opts);
    }
    delete(path, opts) {
      return this.methodRequest("delete", path, opts);
    }
    methodRequest(method, path, opts) {
      return this.request(Promise.resolve(opts).then(async (opts2) => {
        const body = opts2 && isBlobLike(opts2?.body) ? new DataView(await opts2.body.arrayBuffer()) : opts2?.body instanceof DataView ? opts2.body : opts2?.body instanceof ArrayBuffer ? new DataView(opts2.body) : opts2 && ArrayBuffer.isView(opts2?.body) ? new DataView(opts2.body.buffer) : opts2?.body;
        return { method, path, ...opts2, body };
      }));
    }
    getAPIList(path, Page2, opts) {
      return this.requestAPIList(Page2, { method: "get", path, ...opts });
    }
    calculateContentLength(body) {
      if (typeof body === "string") {
        if (typeof Buffer !== "undefined") {
          return Buffer.byteLength(body, "utf8").toString();
        }
        if (typeof TextEncoder !== "undefined") {
          const encoder = new TextEncoder();
          const encoded = encoder.encode(body);
          return encoded.length.toString();
        }
      } else if (ArrayBuffer.isView(body)) {
        return body.byteLength.toString();
      }
      return null;
    }
    buildRequest(options, { retryCount = 0 } = {}) {
      options = { ...options };
      const { method, path, query, headers = {} } = options;
      const body = ArrayBuffer.isView(options.body) || options.__binaryRequest && typeof options.body === "string" ? options.body : isMultipartBody(options.body) ? options.body.body : options.body ? JSON.stringify(options.body, null, 2) : null;
      const contentLength = this.calculateContentLength(body);
      const url = this.buildURL(path, query);
      if ("timeout" in options)
        validatePositiveInteger("timeout", options.timeout);
      options.timeout = options.timeout ?? this.timeout;
      const httpAgent = options.httpAgent ?? this.httpAgent ?? getDefaultAgent(url);
      const minAgentTimeout = options.timeout + 1e3;
      if (typeof httpAgent?.options?.timeout === "number" && minAgentTimeout > (httpAgent.options.timeout ?? 0)) {
        httpAgent.options.timeout = minAgentTimeout;
      }
      if (this.idempotencyHeader && method !== "get") {
        if (!options.idempotencyKey)
          options.idempotencyKey = this.defaultIdempotencyKey();
        headers[this.idempotencyHeader] = options.idempotencyKey;
      }
      const reqHeaders = this.buildHeaders({ options, headers, contentLength, retryCount });
      const req = {
        method,
        ...body && { body },
        headers: reqHeaders,
        ...httpAgent && { agent: httpAgent },
        // @ts-ignore node-fetch uses a custom AbortSignal type that is
        // not compatible with standard web types
        signal: options.signal ?? null
      };
      return { req, url, timeout: options.timeout };
    }
    buildHeaders({ options, headers, contentLength, retryCount }) {
      const reqHeaders = {};
      if (contentLength) {
        reqHeaders["content-length"] = contentLength;
      }
      const defaultHeaders = this.defaultHeaders(options);
      applyHeadersMut(reqHeaders, defaultHeaders);
      applyHeadersMut(reqHeaders, headers);
      if (isMultipartBody(options.body) && kind !== "node") {
        delete reqHeaders["content-type"];
      }
      if (getHeader(defaultHeaders, "x-stainless-retry-count") === void 0 && getHeader(headers, "x-stainless-retry-count") === void 0) {
        reqHeaders["x-stainless-retry-count"] = String(retryCount);
      }
      if (getHeader(defaultHeaders, "x-stainless-timeout") === void 0 && getHeader(headers, "x-stainless-timeout") === void 0 && options.timeout) {
        reqHeaders["x-stainless-timeout"] = String(options.timeout);
      }
      this.validateHeaders(reqHeaders, headers);
      return reqHeaders;
    }
    _calculateNonstreamingTimeout(maxTokens) {
      const defaultTimeout = 10 * 60;
      const expectedTimeout = 60 * 60 * maxTokens / 128e3;
      if (expectedTimeout > defaultTimeout) {
        throw new AnthropicError("Streaming is strongly recommended for operations that may take longer than 10 minutes. See https://github.com/anthropics/anthropic-sdk-python#streaming-responses for more details");
      }
      return defaultTimeout * 1e3;
    }
    /**
     * Used as a callback for mutating the given `FinalRequestOptions` object.
     */
    async prepareOptions(options) {
    }
    /**
     * Used as a callback for mutating the given `RequestInit` object.
     *
     * This is useful for cases where you want to add certain headers based off of
     * the request properties, e.g. `method` or `url`.
     */
    async prepareRequest(request, { url, options }) {
    }
    parseHeaders(headers) {
      return !headers ? {} : Symbol.iterator in headers ? Object.fromEntries(Array.from(headers).map((header) => [...header])) : { ...headers };
    }
    makeStatusError(status, error, message, headers) {
      return APIError.generate(status, error, message, headers);
    }
    request(options, remainingRetries = null) {
      return new APIPromise(this.makeRequest(options, remainingRetries));
    }
    async makeRequest(optionsInput, retriesRemaining) {
      const options = await optionsInput;
      const maxRetries = options.maxRetries ?? this.maxRetries;
      if (retriesRemaining == null) {
        retriesRemaining = maxRetries;
      }
      await this.prepareOptions(options);
      const { req, url, timeout } = this.buildRequest(options, { retryCount: maxRetries - retriesRemaining });
      await this.prepareRequest(req, { url, options });
      debug("request", url, options, req.headers);
      if (options.signal?.aborted) {
        throw new APIUserAbortError();
      }
      const controller = new AbortController();
      const response = await this.fetchWithTimeout(url, req, timeout, controller).catch(castToError);
      if (response instanceof Error) {
        if (options.signal?.aborted) {
          throw new APIUserAbortError();
        }
        if (retriesRemaining) {
          return this.retryRequest(options, retriesRemaining);
        }
        if (response.name === "AbortError") {
          throw new APIConnectionTimeoutError();
        }
        throw new APIConnectionError({ cause: response });
      }
      const responseHeaders = createResponseHeaders(response.headers);
      if (!response.ok) {
        if (retriesRemaining && this.shouldRetry(response)) {
          const retryMessage2 = `retrying, ${retriesRemaining} attempts remaining`;
          debug(`response (error; ${retryMessage2})`, response.status, url, responseHeaders);
          return this.retryRequest(options, retriesRemaining, responseHeaders);
        }
        const errText = await response.text().catch((e) => castToError(e).message);
        const errJSON = safeJSON(errText);
        const errMessage = errJSON ? void 0 : errText;
        const retryMessage = retriesRemaining ? `(error; no more retries left)` : `(error; not retryable)`;
        debug(`response (error; ${retryMessage})`, response.status, url, responseHeaders, errMessage);
        const err = this.makeStatusError(response.status, errJSON, errMessage, responseHeaders);
        throw err;
      }
      return { response, options, controller };
    }
    requestAPIList(Page2, options) {
      const request = this.makeRequest(options, null);
      return new PagePromise(this, request, Page2);
    }
    buildURL(path, query) {
      const url = isAbsoluteURL(path) ? new URL(path) : new URL(this.baseURL + (this.baseURL.endsWith("/") && path.startsWith("/") ? path.slice(1) : path));
      const defaultQuery = this.defaultQuery();
      if (!isEmptyObj(defaultQuery)) {
        query = { ...defaultQuery, ...query };
      }
      if (typeof query === "object" && query && !Array.isArray(query)) {
        url.search = this.stringifyQuery(query);
      }
      return url.toString();
    }
    stringifyQuery(query) {
      return Object.entries(query).filter(([_, value]) => typeof value !== "undefined").map(([key, value]) => {
        if (typeof value === "string" || typeof value === "number" || typeof value === "boolean") {
          return `${encodeURIComponent(key)}=${encodeURIComponent(value)}`;
        }
        if (value === null) {
          return `${encodeURIComponent(key)}=`;
        }
        throw new AnthropicError(`Cannot stringify type ${typeof value}; Expected string, number, boolean, or null. If you need to pass nested query parameters, you can manually encode them, e.g. { query: { 'foo[key1]': value1, 'foo[key2]': value2 } }, and please open a GitHub issue requesting better support for your use case.`);
      }).join("&");
    }
    async fetchWithTimeout(url, init, ms, controller) {
      const { signal, ...options } = init || {};
      if (signal)
        signal.addEventListener("abort", () => controller.abort());
      const timeout = setTimeout(() => controller.abort(), ms);
      const fetchOptions = {
        signal: controller.signal,
        ...options
      };
      if (fetchOptions.method) {
        fetchOptions.method = fetchOptions.method.toUpperCase();
      }
      const socketKeepAliveInterval = 60 * 1e3;
      const keepAliveTimeout = setTimeout(() => {
        if (fetchOptions && fetchOptions?.agent?.sockets) {
          for (const socket of Object.values(fetchOptions?.agent?.sockets).flat()) {
            if (socket?.setKeepAlive) {
              socket.setKeepAlive(true, socketKeepAliveInterval);
            }
          }
        }
      }, socketKeepAliveInterval);
      return (
        // use undefined this binding; fetch errors if bound to something else in browser/cloudflare
        this.fetch.call(void 0, url, fetchOptions).finally(() => {
          clearTimeout(timeout);
          clearTimeout(keepAliveTimeout);
        })
      );
    }
    shouldRetry(response) {
      const shouldRetryHeader = response.headers.get("x-should-retry");
      if (shouldRetryHeader === "true")
        return true;
      if (shouldRetryHeader === "false")
        return false;
      if (response.status === 408)
        return true;
      if (response.status === 409)
        return true;
      if (response.status === 429)
        return true;
      if (response.status >= 500)
        return true;
      return false;
    }
    async retryRequest(options, retriesRemaining, responseHeaders) {
      let timeoutMillis;
      const retryAfterMillisHeader = responseHeaders?.["retry-after-ms"];
      if (retryAfterMillisHeader) {
        const timeoutMs = parseFloat(retryAfterMillisHeader);
        if (!Number.isNaN(timeoutMs)) {
          timeoutMillis = timeoutMs;
        }
      }
      const retryAfterHeader = responseHeaders?.["retry-after"];
      if (retryAfterHeader && !timeoutMillis) {
        const timeoutSeconds = parseFloat(retryAfterHeader);
        if (!Number.isNaN(timeoutSeconds)) {
          timeoutMillis = timeoutSeconds * 1e3;
        } else {
          timeoutMillis = Date.parse(retryAfterHeader) - Date.now();
        }
      }
      if (!(timeoutMillis && 0 <= timeoutMillis && timeoutMillis < 60 * 1e3)) {
        const maxRetries = options.maxRetries ?? this.maxRetries;
        timeoutMillis = this.calculateDefaultRetryTimeoutMillis(retriesRemaining, maxRetries);
      }
      await sleep(timeoutMillis);
      return this.makeRequest(options, retriesRemaining - 1);
    }
    calculateDefaultRetryTimeoutMillis(retriesRemaining, maxRetries) {
      const initialRetryDelay = 0.5;
      const maxRetryDelay = 8;
      const numRetries = maxRetries - retriesRemaining;
      const sleepSeconds = Math.min(initialRetryDelay * Math.pow(2, numRetries), maxRetryDelay);
      const jitter = 1 - Math.random() * 0.25;
      return sleepSeconds * jitter * 1e3;
    }
    getUserAgent() {
      return `${this.constructor.name}/JS ${VERSION}`;
    }
  };
  var AbstractPage = class {
    constructor(client, response, body, options) {
      _AbstractPage_client.set(this, void 0);
      __classPrivateFieldSet2(this, _AbstractPage_client, client, "f");
      this.options = options;
      this.response = response;
      this.body = body;
    }
    hasNextPage() {
      const items = this.getPaginatedItems();
      if (!items.length)
        return false;
      return this.nextPageInfo() != null;
    }
    async getNextPage() {
      const nextInfo = this.nextPageInfo();
      if (!nextInfo) {
        throw new AnthropicError("No next page expected; please check `.hasNextPage()` before calling `.getNextPage()`.");
      }
      const nextOptions = { ...this.options };
      if ("params" in nextInfo && typeof nextOptions.query === "object") {
        nextOptions.query = { ...nextOptions.query, ...nextInfo.params };
      } else if ("url" in nextInfo) {
        const params = [...Object.entries(nextOptions.query || {}), ...nextInfo.url.searchParams.entries()];
        for (const [key, value] of params) {
          nextInfo.url.searchParams.set(key, value);
        }
        nextOptions.query = void 0;
        nextOptions.path = nextInfo.url.toString();
      }
      return await __classPrivateFieldGet2(this, _AbstractPage_client, "f").requestAPIList(this.constructor, nextOptions);
    }
    async *iterPages() {
      let page = this;
      yield page;
      while (page.hasNextPage()) {
        page = await page.getNextPage();
        yield page;
      }
    }
    async *[(_AbstractPage_client = /* @__PURE__ */ new WeakMap(), Symbol.asyncIterator)]() {
      for await (const page of this.iterPages()) {
        for (const item of page.getPaginatedItems()) {
          yield item;
        }
      }
    }
  };
  var PagePromise = class extends APIPromise {
    constructor(client, request, Page2) {
      super(request, async (props) => new Page2(client, props.response, await defaultParseResponse(props), props.options));
    }
    /**
     * Allow auto-paginating iteration on an unawaited list call, eg:
     *
     *    for await (const item of client.items.list()) {
     *      console.log(item)
     *    }
     */
    async *[Symbol.asyncIterator]() {
      const page = await this;
      for await (const item of page) {
        yield item;
      }
    }
  };
  var createResponseHeaders = (headers) => {
    return new Proxy(Object.fromEntries(
      // @ts-ignore
      headers.entries()
    ), {
      get(target, name) {
        const key = name.toString();
        return target[key.toLowerCase()] || target[key];
      }
    });
  };
  var requestOptionsKeys = {
    method: true,
    path: true,
    query: true,
    body: true,
    headers: true,
    maxRetries: true,
    stream: true,
    timeout: true,
    httpAgent: true,
    signal: true,
    idempotencyKey: true,
    __binaryRequest: true,
    __binaryResponse: true,
    __streamClass: true
  };
  var isRequestOptions = (obj) => {
    return typeof obj === "object" && obj !== null && !isEmptyObj(obj) && Object.keys(obj).every((k) => hasOwn(requestOptionsKeys, k));
  };
  var getPlatformProperties = () => {
    if (typeof Deno !== "undefined" && Deno.build != null) {
      return {
        "X-Stainless-Lang": "js",
        "X-Stainless-Package-Version": VERSION,
        "X-Stainless-OS": normalizePlatform(Deno.build.os),
        "X-Stainless-Arch": normalizeArch(Deno.build.arch),
        "X-Stainless-Runtime": "deno",
        "X-Stainless-Runtime-Version": typeof Deno.version === "string" ? Deno.version : Deno.version?.deno ?? "unknown"
      };
    }
    if (typeof EdgeRuntime !== "undefined") {
      return {
        "X-Stainless-Lang": "js",
        "X-Stainless-Package-Version": VERSION,
        "X-Stainless-OS": "Unknown",
        "X-Stainless-Arch": `other:${EdgeRuntime}`,
        "X-Stainless-Runtime": "edge",
        "X-Stainless-Runtime-Version": process.version
      };
    }
    if (Object.prototype.toString.call(typeof process !== "undefined" ? process : 0) === "[object process]") {
      return {
        "X-Stainless-Lang": "js",
        "X-Stainless-Package-Version": VERSION,
        "X-Stainless-OS": normalizePlatform(process.platform),
        "X-Stainless-Arch": normalizeArch(process.arch),
        "X-Stainless-Runtime": "node",
        "X-Stainless-Runtime-Version": process.version
      };
    }
    const browserInfo = getBrowserInfo();
    if (browserInfo) {
      return {
        "X-Stainless-Lang": "js",
        "X-Stainless-Package-Version": VERSION,
        "X-Stainless-OS": "Unknown",
        "X-Stainless-Arch": "unknown",
        "X-Stainless-Runtime": `browser:${browserInfo.browser}`,
        "X-Stainless-Runtime-Version": browserInfo.version
      };
    }
    return {
      "X-Stainless-Lang": "js",
      "X-Stainless-Package-Version": VERSION,
      "X-Stainless-OS": "Unknown",
      "X-Stainless-Arch": "unknown",
      "X-Stainless-Runtime": "unknown",
      "X-Stainless-Runtime-Version": "unknown"
    };
  };
  function getBrowserInfo() {
    if (typeof navigator === "undefined" || !navigator) {
      return null;
    }
    const browserPatterns = [
      { key: "edge", pattern: /Edge(?:\W+(\d+)\.(\d+)(?:\.(\d+))?)?/ },
      { key: "ie", pattern: /MSIE(?:\W+(\d+)\.(\d+)(?:\.(\d+))?)?/ },
      { key: "ie", pattern: /Trident(?:.*rv\:(\d+)\.(\d+)(?:\.(\d+))?)?/ },
      { key: "chrome", pattern: /Chrome(?:\W+(\d+)\.(\d+)(?:\.(\d+))?)?/ },
      { key: "firefox", pattern: /Firefox(?:\W+(\d+)\.(\d+)(?:\.(\d+))?)?/ },
      { key: "safari", pattern: /(?:Version\W+(\d+)\.(\d+)(?:\.(\d+))?)?(?:\W+Mobile\S*)?\W+Safari/ }
    ];
    for (const { key, pattern } of browserPatterns) {
      const match = pattern.exec(navigator.userAgent);
      if (match) {
        const major = match[1] || 0;
        const minor = match[2] || 0;
        const patch = match[3] || 0;
        return { browser: key, version: `${major}.${minor}.${patch}` };
      }
    }
    return null;
  }
  var normalizeArch = (arch) => {
    if (arch === "x32")
      return "x32";
    if (arch === "x86_64" || arch === "x64")
      return "x64";
    if (arch === "arm")
      return "arm";
    if (arch === "aarch64" || arch === "arm64")
      return "arm64";
    if (arch)
      return `other:${arch}`;
    return "unknown";
  };
  var normalizePlatform = (platform) => {
    platform = platform.toLowerCase();
    if (platform.includes("ios"))
      return "iOS";
    if (platform === "android")
      return "Android";
    if (platform === "darwin")
      return "MacOS";
    if (platform === "win32")
      return "Windows";
    if (platform === "freebsd")
      return "FreeBSD";
    if (platform === "openbsd")
      return "OpenBSD";
    if (platform === "linux")
      return "Linux";
    if (platform)
      return `Other:${platform}`;
    return "Unknown";
  };
  var _platformHeaders;
  var getPlatformHeaders = () => {
    return _platformHeaders ?? (_platformHeaders = getPlatformProperties());
  };
  var safeJSON = (text) => {
    try {
      return JSON.parse(text);
    } catch (err) {
      return void 0;
    }
  };
  var startsWithSchemeRegexp = /^[a-z][a-z0-9+.-]*:/i;
  var isAbsoluteURL = (url) => {
    return startsWithSchemeRegexp.test(url);
  };
  var sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
  var validatePositiveInteger = (name, n) => {
    if (typeof n !== "number" || !Number.isInteger(n)) {
      throw new AnthropicError(`${name} must be an integer`);
    }
    if (n < 0) {
      throw new AnthropicError(`${name} must be a positive integer`);
    }
    return n;
  };
  var castToError = (err) => {
    if (err instanceof Error)
      return err;
    if (typeof err === "object" && err !== null) {
      try {
        return new Error(JSON.stringify(err));
      } catch {
      }
    }
    return new Error(String(err));
  };
  var readEnv = (env) => {
    if (typeof process !== "undefined") {
      return process.env?.[env]?.trim() ?? void 0;
    }
    if (typeof Deno !== "undefined") {
      return Deno.env?.get?.(env)?.trim();
    }
    return void 0;
  };
  function isEmptyObj(obj) {
    if (!obj)
      return true;
    for (const _k in obj)
      return false;
    return true;
  }
  function hasOwn(obj, key) {
    return Object.prototype.hasOwnProperty.call(obj, key);
  }
  function applyHeadersMut(targetHeaders, newHeaders) {
    for (const k in newHeaders) {
      if (!hasOwn(newHeaders, k))
        continue;
      const lowerKey = k.toLowerCase();
      if (!lowerKey)
        continue;
      const val = newHeaders[k];
      if (val === null) {
        delete targetHeaders[lowerKey];
      } else if (val !== void 0) {
        targetHeaders[lowerKey] = val;
      }
    }
  }
  function debug(action, ...args) {
    if (typeof process !== "undefined" && process?.env?.["DEBUG"] === "true") {
      console.log(`Anthropic:DEBUG:${action}`, ...args);
    }
  }
  var uuid4 = () => {
    return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, (c) => {
      const r = Math.random() * 16 | 0;
      const v = c === "x" ? r : r & 3 | 8;
      return v.toString(16);
    });
  };
  var isRunningInBrowser = () => {
    return (
      // @ts-ignore
      typeof window !== "undefined" && // @ts-ignore
      typeof window.document !== "undefined" && // @ts-ignore
      typeof navigator !== "undefined"
    );
  };
  var isHeadersProtocol = (headers) => {
    return typeof headers?.get === "function";
  };
  var getHeader = (headers, header) => {
    const lowerCasedHeader = header.toLowerCase();
    if (isHeadersProtocol(headers)) {
      const intercapsHeader = header[0]?.toUpperCase() + header.substring(1).replace(/([^\w])(\w)/g, (_m, g1, g2) => g1 + g2.toUpperCase());
      for (const key of [header, lowerCasedHeader, header.toUpperCase(), intercapsHeader]) {
        const value = headers.get(key);
        if (value) {
          return value;
        }
      }
    }
    for (const [key, value] of Object.entries(headers)) {
      if (key.toLowerCase() === lowerCasedHeader) {
        if (Array.isArray(value)) {
          if (value.length <= 1)
            return value[0];
          console.warn(`Received ${value.length} entries for the ${header} header, using the first entry.`);
          return value[0];
        }
        return value;
      }
    }
    return void 0;
  };

  // ../../node_modules/@anthropic-ai/sdk/pagination.mjs
  var Page = class extends AbstractPage {
    constructor(client, response, body, options) {
      super(client, response, body, options);
      this.data = body.data || [];
      this.has_more = body.has_more || false;
      this.first_id = body.first_id || null;
      this.last_id = body.last_id || null;
    }
    getPaginatedItems() {
      return this.data ?? [];
    }
    hasNextPage() {
      if (this.has_more === false) {
        return false;
      }
      return super.hasNextPage();
    }
    // @deprecated Please use `nextPageInfo()` instead
    nextPageParams() {
      const info = this.nextPageInfo();
      if (!info)
        return null;
      if ("params" in info)
        return info.params;
      const params = Object.fromEntries(info.url.searchParams);
      if (!Object.keys(params).length)
        return null;
      return params;
    }
    nextPageInfo() {
      if (this.options.query?.["before_id"]) {
        const firstId = this.first_id;
        if (!firstId) {
          return null;
        }
        return {
          params: {
            before_id: firstId
          }
        };
      }
      const cursor = this.last_id;
      if (!cursor) {
        return null;
      }
      return {
        params: {
          after_id: cursor
        }
      };
    }
  };

  // ../../node_modules/@anthropic-ai/sdk/resource.mjs
  var APIResource = class {
    constructor(client) {
      this._client = client;
    }
  };

  // ../../node_modules/@anthropic-ai/sdk/resources/beta/models.mjs
  var Models = class extends APIResource {
    /**
     * Get a specific model.
     *
     * The Models API response can be used to determine information about a specific
     * model or resolve a model alias to a model ID.
     */
    retrieve(modelId, options) {
      return this._client.get(`/v1/models/${modelId}?beta=true`, options);
    }
    list(query = {}, options) {
      if (isRequestOptions(query)) {
        return this.list({}, query);
      }
      return this._client.getAPIList("/v1/models?beta=true", BetaModelInfosPage, { query, ...options });
    }
  };
  var BetaModelInfosPage = class extends Page {
  };
  Models.BetaModelInfosPage = BetaModelInfosPage;

  // ../../node_modules/@anthropic-ai/sdk/internal/decoders/jsonl.mjs
  var JSONLDecoder = class _JSONLDecoder {
    constructor(iterator, controller) {
      this.iterator = iterator;
      this.controller = controller;
    }
    async *decoder() {
      const lineDecoder = new LineDecoder();
      for await (const chunk of this.iterator) {
        for (const line of lineDecoder.decode(chunk)) {
          yield JSON.parse(line);
        }
      }
      for (const line of lineDecoder.flush()) {
        yield JSON.parse(line);
      }
    }
    [Symbol.asyncIterator]() {
      return this.decoder();
    }
    static fromResponse(response, controller) {
      if (!response.body) {
        controller.abort();
        throw new AnthropicError(`Attempted to iterate over a response with no body`);
      }
      return new _JSONLDecoder(ReadableStreamToAsyncIterable(response.body), controller);
    }
  };

  // ../../node_modules/@anthropic-ai/sdk/resources/beta/messages/batches.mjs
  var Batches = class extends APIResource {
    /**
     * Send a batch of Message creation requests.
     *
     * The Message Batches API can be used to process multiple Messages API requests at
     * once. Once a Message Batch is created, it begins processing immediately. Batches
     * can take up to 24 hours to complete.
     *
     * Learn more about the Message Batches API in our
     * [user guide](/en/docs/build-with-claude/batch-processing)
     */
    create(params, options) {
      const { betas, ...body } = params;
      return this._client.post("/v1/messages/batches?beta=true", {
        body,
        ...options,
        headers: {
          "anthropic-beta": [...betas ?? [], "message-batches-2024-09-24"].toString(),
          ...options?.headers
        }
      });
    }
    retrieve(messageBatchId, params = {}, options) {
      if (isRequestOptions(params)) {
        return this.retrieve(messageBatchId, {}, params);
      }
      const { betas } = params;
      return this._client.get(`/v1/messages/batches/${messageBatchId}?beta=true`, {
        ...options,
        headers: {
          "anthropic-beta": [...betas ?? [], "message-batches-2024-09-24"].toString(),
          ...options?.headers
        }
      });
    }
    list(params = {}, options) {
      if (isRequestOptions(params)) {
        return this.list({}, params);
      }
      const { betas, ...query } = params;
      return this._client.getAPIList("/v1/messages/batches?beta=true", BetaMessageBatchesPage, {
        query,
        ...options,
        headers: {
          "anthropic-beta": [...betas ?? [], "message-batches-2024-09-24"].toString(),
          ...options?.headers
        }
      });
    }
    delete(messageBatchId, params = {}, options) {
      if (isRequestOptions(params)) {
        return this.delete(messageBatchId, {}, params);
      }
      const { betas } = params;
      return this._client.delete(`/v1/messages/batches/${messageBatchId}?beta=true`, {
        ...options,
        headers: {
          "anthropic-beta": [...betas ?? [], "message-batches-2024-09-24"].toString(),
          ...options?.headers
        }
      });
    }
    cancel(messageBatchId, params = {}, options) {
      if (isRequestOptions(params)) {
        return this.cancel(messageBatchId, {}, params);
      }
      const { betas } = params;
      return this._client.post(`/v1/messages/batches/${messageBatchId}/cancel?beta=true`, {
        ...options,
        headers: {
          "anthropic-beta": [...betas ?? [], "message-batches-2024-09-24"].toString(),
          ...options?.headers
        }
      });
    }
    async results(messageBatchId, params = {}, options) {
      if (isRequestOptions(params)) {
        return this.results(messageBatchId, {}, params);
      }
      const batch = await this.retrieve(messageBatchId);
      if (!batch.results_url) {
        throw new AnthropicError(`No batch \`results_url\`; Has it finished processing? ${batch.processing_status} - ${batch.id}`);
      }
      const { betas } = params;
      return this._client.get(batch.results_url, {
        ...options,
        headers: {
          "anthropic-beta": [...betas ?? [], "message-batches-2024-09-24"].toString(),
          Accept: "application/binary",
          ...options?.headers
        },
        __binaryResponse: true
      })._thenUnwrap((_, props) => JSONLDecoder.fromResponse(props.response, props.controller));
    }
  };
  var BetaMessageBatchesPage = class extends Page {
  };
  Batches.BetaMessageBatchesPage = BetaMessageBatchesPage;

  // ../../node_modules/@anthropic-ai/sdk/_vendor/partial-json-parser/parser.mjs
  var tokenize = (input) => {
    let current = 0;
    let tokens = [];
    while (current < input.length) {
      let char = input[current];
      if (char === "\\") {
        current++;
        continue;
      }
      if (char === "{") {
        tokens.push({
          type: "brace",
          value: "{"
        });
        current++;
        continue;
      }
      if (char === "}") {
        tokens.push({
          type: "brace",
          value: "}"
        });
        current++;
        continue;
      }
      if (char === "[") {
        tokens.push({
          type: "paren",
          value: "["
        });
        current++;
        continue;
      }
      if (char === "]") {
        tokens.push({
          type: "paren",
          value: "]"
        });
        current++;
        continue;
      }
      if (char === ":") {
        tokens.push({
          type: "separator",
          value: ":"
        });
        current++;
        continue;
      }
      if (char === ",") {
        tokens.push({
          type: "delimiter",
          value: ","
        });
        current++;
        continue;
      }
      if (char === '"') {
        let value = "";
        let danglingQuote = false;
        char = input[++current];
        while (char !== '"') {
          if (current === input.length) {
            danglingQuote = true;
            break;
          }
          if (char === "\\") {
            current++;
            if (current === input.length) {
              danglingQuote = true;
              break;
            }
            value += char + input[current];
            char = input[++current];
          } else {
            value += char;
            char = input[++current];
          }
        }
        char = input[++current];
        if (!danglingQuote) {
          tokens.push({
            type: "string",
            value
          });
        }
        continue;
      }
      let WHITESPACE = /\s/;
      if (char && WHITESPACE.test(char)) {
        current++;
        continue;
      }
      let NUMBERS = /[0-9]/;
      if (char && NUMBERS.test(char) || char === "-" || char === ".") {
        let value = "";
        if (char === "-") {
          value += char;
          char = input[++current];
        }
        while (char && NUMBERS.test(char) || char === ".") {
          value += char;
          char = input[++current];
        }
        tokens.push({
          type: "number",
          value
        });
        continue;
      }
      let LETTERS = /[a-z]/i;
      if (char && LETTERS.test(char)) {
        let value = "";
        while (char && LETTERS.test(char)) {
          if (current === input.length) {
            break;
          }
          value += char;
          char = input[++current];
        }
        if (value == "true" || value == "false" || value === "null") {
          tokens.push({
            type: "name",
            value
          });
        } else {
          current++;
          continue;
        }
        continue;
      }
      current++;
    }
    return tokens;
  };
  var strip = (tokens) => {
    if (tokens.length === 0) {
      return tokens;
    }
    let lastToken = tokens[tokens.length - 1];
    switch (lastToken.type) {
      case "separator":
        tokens = tokens.slice(0, tokens.length - 1);
        return strip(tokens);
        break;
      case "number":
        let lastCharacterOfLastToken = lastToken.value[lastToken.value.length - 1];
        if (lastCharacterOfLastToken === "." || lastCharacterOfLastToken === "-") {
          tokens = tokens.slice(0, tokens.length - 1);
          return strip(tokens);
        }
      case "string":
        let tokenBeforeTheLastToken = tokens[tokens.length - 2];
        if (tokenBeforeTheLastToken?.type === "delimiter") {
          tokens = tokens.slice(0, tokens.length - 1);
          return strip(tokens);
        } else if (tokenBeforeTheLastToken?.type === "brace" && tokenBeforeTheLastToken.value === "{") {
          tokens = tokens.slice(0, tokens.length - 1);
          return strip(tokens);
        }
        break;
      case "delimiter":
        tokens = tokens.slice(0, tokens.length - 1);
        return strip(tokens);
        break;
    }
    return tokens;
  };
  var unstrip = (tokens) => {
    let tail = [];
    tokens.map((token) => {
      if (token.type === "brace") {
        if (token.value === "{") {
          tail.push("}");
        } else {
          tail.splice(tail.lastIndexOf("}"), 1);
        }
      }
      if (token.type === "paren") {
        if (token.value === "[") {
          tail.push("]");
        } else {
          tail.splice(tail.lastIndexOf("]"), 1);
        }
      }
    });
    if (tail.length > 0) {
      tail.reverse().map((item) => {
        if (item === "}") {
          tokens.push({
            type: "brace",
            value: "}"
          });
        } else if (item === "]") {
          tokens.push({
            type: "paren",
            value: "]"
          });
        }
      });
    }
    return tokens;
  };
  var generate = (tokens) => {
    let output = "";
    tokens.map((token) => {
      switch (token.type) {
        case "string":
          output += '"' + token.value + '"';
          break;
        default:
          output += token.value;
          break;
      }
    });
    return output;
  };
  var partialParse2 = (input) => JSON.parse(generate(unstrip(strip(tokenize(input)))));

  // ../../node_modules/@anthropic-ai/sdk/lib/BetaMessageStream.mjs
  var __classPrivateFieldSet3 = function(receiver, state, value, kind2, f) {
    if (kind2 === "m") throw new TypeError("Private method is not writable");
    if (kind2 === "a" && !f) throw new TypeError("Private accessor was defined without a setter");
    if (typeof state === "function" ? receiver !== state || !f : !state.has(receiver)) throw new TypeError("Cannot write private member to an object whose class did not declare it");
    return kind2 === "a" ? f.call(receiver, value) : f ? f.value = value : state.set(receiver, value), value;
  };
  var __classPrivateFieldGet3 = function(receiver, state, kind2, f) {
    if (kind2 === "a" && !f) throw new TypeError("Private accessor was defined without a getter");
    if (typeof state === "function" ? receiver !== state || !f : !state.has(receiver)) throw new TypeError("Cannot read private member from an object whose class did not declare it");
    return kind2 === "m" ? f : kind2 === "a" ? f.call(receiver) : f ? f.value : state.get(receiver);
  };
  var _BetaMessageStream_instances;
  var _BetaMessageStream_currentMessageSnapshot;
  var _BetaMessageStream_connectedPromise;
  var _BetaMessageStream_resolveConnectedPromise;
  var _BetaMessageStream_rejectConnectedPromise;
  var _BetaMessageStream_endPromise;
  var _BetaMessageStream_resolveEndPromise;
  var _BetaMessageStream_rejectEndPromise;
  var _BetaMessageStream_listeners;
  var _BetaMessageStream_ended;
  var _BetaMessageStream_errored;
  var _BetaMessageStream_aborted;
  var _BetaMessageStream_catchingPromiseCreated;
  var _BetaMessageStream_response;
  var _BetaMessageStream_request_id;
  var _BetaMessageStream_getFinalMessage;
  var _BetaMessageStream_getFinalText;
  var _BetaMessageStream_handleError;
  var _BetaMessageStream_beginRequest;
  var _BetaMessageStream_addStreamEvent;
  var _BetaMessageStream_endRequest;
  var _BetaMessageStream_accumulateMessage;
  var JSON_BUF_PROPERTY = "__json_buf";
  var BetaMessageStream = class _BetaMessageStream {
    constructor() {
      _BetaMessageStream_instances.add(this);
      this.messages = [];
      this.receivedMessages = [];
      _BetaMessageStream_currentMessageSnapshot.set(this, void 0);
      this.controller = new AbortController();
      _BetaMessageStream_connectedPromise.set(this, void 0);
      _BetaMessageStream_resolveConnectedPromise.set(this, () => {
      });
      _BetaMessageStream_rejectConnectedPromise.set(this, () => {
      });
      _BetaMessageStream_endPromise.set(this, void 0);
      _BetaMessageStream_resolveEndPromise.set(this, () => {
      });
      _BetaMessageStream_rejectEndPromise.set(this, () => {
      });
      _BetaMessageStream_listeners.set(this, {});
      _BetaMessageStream_ended.set(this, false);
      _BetaMessageStream_errored.set(this, false);
      _BetaMessageStream_aborted.set(this, false);
      _BetaMessageStream_catchingPromiseCreated.set(this, false);
      _BetaMessageStream_response.set(this, void 0);
      _BetaMessageStream_request_id.set(this, void 0);
      _BetaMessageStream_handleError.set(this, (error) => {
        __classPrivateFieldSet3(this, _BetaMessageStream_errored, true, "f");
        if (error instanceof Error && error.name === "AbortError") {
          error = new APIUserAbortError();
        }
        if (error instanceof APIUserAbortError) {
          __classPrivateFieldSet3(this, _BetaMessageStream_aborted, true, "f");
          return this._emit("abort", error);
        }
        if (error instanceof AnthropicError) {
          return this._emit("error", error);
        }
        if (error instanceof Error) {
          const anthropicError = new AnthropicError(error.message);
          anthropicError.cause = error;
          return this._emit("error", anthropicError);
        }
        return this._emit("error", new AnthropicError(String(error)));
      });
      __classPrivateFieldSet3(this, _BetaMessageStream_connectedPromise, new Promise((resolve, reject) => {
        __classPrivateFieldSet3(this, _BetaMessageStream_resolveConnectedPromise, resolve, "f");
        __classPrivateFieldSet3(this, _BetaMessageStream_rejectConnectedPromise, reject, "f");
      }), "f");
      __classPrivateFieldSet3(this, _BetaMessageStream_endPromise, new Promise((resolve, reject) => {
        __classPrivateFieldSet3(this, _BetaMessageStream_resolveEndPromise, resolve, "f");
        __classPrivateFieldSet3(this, _BetaMessageStream_rejectEndPromise, reject, "f");
      }), "f");
      __classPrivateFieldGet3(this, _BetaMessageStream_connectedPromise, "f").catch(() => {
      });
      __classPrivateFieldGet3(this, _BetaMessageStream_endPromise, "f").catch(() => {
      });
    }
    get response() {
      return __classPrivateFieldGet3(this, _BetaMessageStream_response, "f");
    }
    get request_id() {
      return __classPrivateFieldGet3(this, _BetaMessageStream_request_id, "f");
    }
    /**
     * Returns the `MessageStream` data, the raw `Response` instance and the ID of the request,
     * returned vie the `request-id` header which is useful for debugging requests and resporting
     * issues to Anthropic.
     *
     * This is the same as the `APIPromise.withResponse()` method.
     *
     * This method will raise an error if you created the stream using `MessageStream.fromReadableStream`
     * as no `Response` is available.
     */
    async withResponse() {
      const response = await __classPrivateFieldGet3(this, _BetaMessageStream_connectedPromise, "f");
      if (!response) {
        throw new Error("Could not resolve a `Response` object");
      }
      return {
        data: this,
        response,
        request_id: response.headers.get("request-id")
      };
    }
    /**
     * Intended for use on the frontend, consuming a stream produced with
     * `.toReadableStream()` on the backend.
     *
     * Note that messages sent to the model do not appear in `.on('message')`
     * in this context.
     */
    static fromReadableStream(stream) {
      const runner = new _BetaMessageStream();
      runner._run(() => runner._fromReadableStream(stream));
      return runner;
    }
    static createMessage(messages, params, options) {
      const runner = new _BetaMessageStream();
      for (const message of params.messages) {
        runner._addMessageParam(message);
      }
      runner._run(() => runner._createMessage(messages, { ...params, stream: true }, { ...options, headers: { ...options?.headers, "X-Stainless-Helper-Method": "stream" } }));
      return runner;
    }
    _run(executor) {
      executor().then(() => {
        this._emitFinal();
        this._emit("end");
      }, __classPrivateFieldGet3(this, _BetaMessageStream_handleError, "f"));
    }
    _addMessageParam(message) {
      this.messages.push(message);
    }
    _addMessage(message, emit = true) {
      this.receivedMessages.push(message);
      if (emit) {
        this._emit("message", message);
      }
    }
    async _createMessage(messages, params, options) {
      const signal = options?.signal;
      if (signal) {
        if (signal.aborted)
          this.controller.abort();
        signal.addEventListener("abort", () => this.controller.abort());
      }
      __classPrivateFieldGet3(this, _BetaMessageStream_instances, "m", _BetaMessageStream_beginRequest).call(this);
      const { response, data: stream } = await messages.create({ ...params, stream: true }, { ...options, signal: this.controller.signal }).withResponse();
      this._connected(response);
      for await (const event of stream) {
        __classPrivateFieldGet3(this, _BetaMessageStream_instances, "m", _BetaMessageStream_addStreamEvent).call(this, event);
      }
      if (stream.controller.signal?.aborted) {
        throw new APIUserAbortError();
      }
      __classPrivateFieldGet3(this, _BetaMessageStream_instances, "m", _BetaMessageStream_endRequest).call(this);
    }
    _connected(response) {
      if (this.ended)
        return;
      __classPrivateFieldSet3(this, _BetaMessageStream_response, response, "f");
      __classPrivateFieldSet3(this, _BetaMessageStream_request_id, response?.headers.get("request-id"), "f");
      __classPrivateFieldGet3(this, _BetaMessageStream_resolveConnectedPromise, "f").call(this, response);
      this._emit("connect");
    }
    get ended() {
      return __classPrivateFieldGet3(this, _BetaMessageStream_ended, "f");
    }
    get errored() {
      return __classPrivateFieldGet3(this, _BetaMessageStream_errored, "f");
    }
    get aborted() {
      return __classPrivateFieldGet3(this, _BetaMessageStream_aborted, "f");
    }
    abort() {
      this.controller.abort();
    }
    /**
     * Adds the listener function to the end of the listeners array for the event.
     * No checks are made to see if the listener has already been added. Multiple calls passing
     * the same combination of event and listener will result in the listener being added, and
     * called, multiple times.
     * @returns this MessageStream, so that calls can be chained
     */
    on(event, listener) {
      const listeners = __classPrivateFieldGet3(this, _BetaMessageStream_listeners, "f")[event] || (__classPrivateFieldGet3(this, _BetaMessageStream_listeners, "f")[event] = []);
      listeners.push({ listener });
      return this;
    }
    /**
     * Removes the specified listener from the listener array for the event.
     * off() will remove, at most, one instance of a listener from the listener array. If any single
     * listener has been added multiple times to the listener array for the specified event, then
     * off() must be called multiple times to remove each instance.
     * @returns this MessageStream, so that calls can be chained
     */
    off(event, listener) {
      const listeners = __classPrivateFieldGet3(this, _BetaMessageStream_listeners, "f")[event];
      if (!listeners)
        return this;
      const index = listeners.findIndex((l) => l.listener === listener);
      if (index >= 0)
        listeners.splice(index, 1);
      return this;
    }
    /**
     * Adds a one-time listener function for the event. The next time the event is triggered,
     * this listener is removed and then invoked.
     * @returns this MessageStream, so that calls can be chained
     */
    once(event, listener) {
      const listeners = __classPrivateFieldGet3(this, _BetaMessageStream_listeners, "f")[event] || (__classPrivateFieldGet3(this, _BetaMessageStream_listeners, "f")[event] = []);
      listeners.push({ listener, once: true });
      return this;
    }
    /**
     * This is similar to `.once()`, but returns a Promise that resolves the next time
     * the event is triggered, instead of calling a listener callback.
     * @returns a Promise that resolves the next time given event is triggered,
     * or rejects if an error is emitted.  (If you request the 'error' event,
     * returns a promise that resolves with the error).
     *
     * Example:
     *
     *   const message = await stream.emitted('message') // rejects if the stream errors
     */
    emitted(event) {
      return new Promise((resolve, reject) => {
        __classPrivateFieldSet3(this, _BetaMessageStream_catchingPromiseCreated, true, "f");
        if (event !== "error")
          this.once("error", reject);
        this.once(event, resolve);
      });
    }
    async done() {
      __classPrivateFieldSet3(this, _BetaMessageStream_catchingPromiseCreated, true, "f");
      await __classPrivateFieldGet3(this, _BetaMessageStream_endPromise, "f");
    }
    get currentMessage() {
      return __classPrivateFieldGet3(this, _BetaMessageStream_currentMessageSnapshot, "f");
    }
    /**
     * @returns a promise that resolves with the the final assistant Message response,
     * or rejects if an error occurred or the stream ended prematurely without producing a Message.
     */
    async finalMessage() {
      await this.done();
      return __classPrivateFieldGet3(this, _BetaMessageStream_instances, "m", _BetaMessageStream_getFinalMessage).call(this);
    }
    /**
     * @returns a promise that resolves with the the final assistant Message's text response, concatenated
     * together if there are more than one text blocks.
     * Rejects if an error occurred or the stream ended prematurely without producing a Message.
     */
    async finalText() {
      await this.done();
      return __classPrivateFieldGet3(this, _BetaMessageStream_instances, "m", _BetaMessageStream_getFinalText).call(this);
    }
    _emit(event, ...args) {
      if (__classPrivateFieldGet3(this, _BetaMessageStream_ended, "f"))
        return;
      if (event === "end") {
        __classPrivateFieldSet3(this, _BetaMessageStream_ended, true, "f");
        __classPrivateFieldGet3(this, _BetaMessageStream_resolveEndPromise, "f").call(this);
      }
      const listeners = __classPrivateFieldGet3(this, _BetaMessageStream_listeners, "f")[event];
      if (listeners) {
        __classPrivateFieldGet3(this, _BetaMessageStream_listeners, "f")[event] = listeners.filter((l) => !l.once);
        listeners.forEach(({ listener }) => listener(...args));
      }
      if (event === "abort") {
        const error = args[0];
        if (!__classPrivateFieldGet3(this, _BetaMessageStream_catchingPromiseCreated, "f") && !listeners?.length) {
          Promise.reject(error);
        }
        __classPrivateFieldGet3(this, _BetaMessageStream_rejectConnectedPromise, "f").call(this, error);
        __classPrivateFieldGet3(this, _BetaMessageStream_rejectEndPromise, "f").call(this, error);
        this._emit("end");
        return;
      }
      if (event === "error") {
        const error = args[0];
        if (!__classPrivateFieldGet3(this, _BetaMessageStream_catchingPromiseCreated, "f") && !listeners?.length) {
          Promise.reject(error);
        }
        __classPrivateFieldGet3(this, _BetaMessageStream_rejectConnectedPromise, "f").call(this, error);
        __classPrivateFieldGet3(this, _BetaMessageStream_rejectEndPromise, "f").call(this, error);
        this._emit("end");
      }
    }
    _emitFinal() {
      const finalMessage = this.receivedMessages.at(-1);
      if (finalMessage) {
        this._emit("finalMessage", __classPrivateFieldGet3(this, _BetaMessageStream_instances, "m", _BetaMessageStream_getFinalMessage).call(this));
      }
    }
    async _fromReadableStream(readableStream, options) {
      const signal = options?.signal;
      if (signal) {
        if (signal.aborted)
          this.controller.abort();
        signal.addEventListener("abort", () => this.controller.abort());
      }
      __classPrivateFieldGet3(this, _BetaMessageStream_instances, "m", _BetaMessageStream_beginRequest).call(this);
      this._connected(null);
      const stream = Stream.fromReadableStream(readableStream, this.controller);
      for await (const event of stream) {
        __classPrivateFieldGet3(this, _BetaMessageStream_instances, "m", _BetaMessageStream_addStreamEvent).call(this, event);
      }
      if (stream.controller.signal?.aborted) {
        throw new APIUserAbortError();
      }
      __classPrivateFieldGet3(this, _BetaMessageStream_instances, "m", _BetaMessageStream_endRequest).call(this);
    }
    [(_BetaMessageStream_currentMessageSnapshot = /* @__PURE__ */ new WeakMap(), _BetaMessageStream_connectedPromise = /* @__PURE__ */ new WeakMap(), _BetaMessageStream_resolveConnectedPromise = /* @__PURE__ */ new WeakMap(), _BetaMessageStream_rejectConnectedPromise = /* @__PURE__ */ new WeakMap(), _BetaMessageStream_endPromise = /* @__PURE__ */ new WeakMap(), _BetaMessageStream_resolveEndPromise = /* @__PURE__ */ new WeakMap(), _BetaMessageStream_rejectEndPromise = /* @__PURE__ */ new WeakMap(), _BetaMessageStream_listeners = /* @__PURE__ */ new WeakMap(), _BetaMessageStream_ended = /* @__PURE__ */ new WeakMap(), _BetaMessageStream_errored = /* @__PURE__ */ new WeakMap(), _BetaMessageStream_aborted = /* @__PURE__ */ new WeakMap(), _BetaMessageStream_catchingPromiseCreated = /* @__PURE__ */ new WeakMap(), _BetaMessageStream_response = /* @__PURE__ */ new WeakMap(), _BetaMessageStream_request_id = /* @__PURE__ */ new WeakMap(), _BetaMessageStream_handleError = /* @__PURE__ */ new WeakMap(), _BetaMessageStream_instances = /* @__PURE__ */ new WeakSet(), _BetaMessageStream_getFinalMessage = function _BetaMessageStream_getFinalMessage2() {
      if (this.receivedMessages.length === 0) {
        throw new AnthropicError("stream ended without producing a Message with role=assistant");
      }
      return this.receivedMessages.at(-1);
    }, _BetaMessageStream_getFinalText = function _BetaMessageStream_getFinalText2() {
      if (this.receivedMessages.length === 0) {
        throw new AnthropicError("stream ended without producing a Message with role=assistant");
      }
      const textBlocks = this.receivedMessages.at(-1).content.filter((block) => block.type === "text").map((block) => block.text);
      if (textBlocks.length === 0) {
        throw new AnthropicError("stream ended without producing a content block with type=text");
      }
      return textBlocks.join(" ");
    }, _BetaMessageStream_beginRequest = function _BetaMessageStream_beginRequest2() {
      if (this.ended)
        return;
      __classPrivateFieldSet3(this, _BetaMessageStream_currentMessageSnapshot, void 0, "f");
    }, _BetaMessageStream_addStreamEvent = function _BetaMessageStream_addStreamEvent2(event) {
      if (this.ended)
        return;
      const messageSnapshot = __classPrivateFieldGet3(this, _BetaMessageStream_instances, "m", _BetaMessageStream_accumulateMessage).call(this, event);
      this._emit("streamEvent", event, messageSnapshot);
      switch (event.type) {
        case "content_block_delta": {
          const content = messageSnapshot.content.at(-1);
          switch (event.delta.type) {
            case "text_delta": {
              if (content.type === "text") {
                this._emit("text", event.delta.text, content.text || "");
              }
              break;
            }
            case "citations_delta": {
              if (content.type === "text") {
                this._emit("citation", event.delta.citation, content.citations ?? []);
              }
              break;
            }
            case "input_json_delta": {
              if (content.type === "tool_use" && content.input) {
                this._emit("inputJson", event.delta.partial_json, content.input);
              }
              break;
            }
            case "thinking_delta": {
              if (content.type === "thinking") {
                this._emit("thinking", event.delta.thinking, content.thinking);
              }
              break;
            }
            case "signature_delta": {
              if (content.type === "thinking") {
                this._emit("signature", content.signature);
              }
              break;
            }
            default:
              checkNever(event.delta);
          }
          break;
        }
        case "message_stop": {
          this._addMessageParam(messageSnapshot);
          this._addMessage(messageSnapshot, true);
          break;
        }
        case "content_block_stop": {
          this._emit("contentBlock", messageSnapshot.content.at(-1));
          break;
        }
        case "message_start": {
          __classPrivateFieldSet3(this, _BetaMessageStream_currentMessageSnapshot, messageSnapshot, "f");
          break;
        }
        case "content_block_start":
        case "message_delta":
          break;
      }
    }, _BetaMessageStream_endRequest = function _BetaMessageStream_endRequest2() {
      if (this.ended) {
        throw new AnthropicError(`stream has ended, this shouldn't happen`);
      }
      const snapshot = __classPrivateFieldGet3(this, _BetaMessageStream_currentMessageSnapshot, "f");
      if (!snapshot) {
        throw new AnthropicError(`request ended without sending any chunks`);
      }
      __classPrivateFieldSet3(this, _BetaMessageStream_currentMessageSnapshot, void 0, "f");
      return snapshot;
    }, _BetaMessageStream_accumulateMessage = function _BetaMessageStream_accumulateMessage2(event) {
      let snapshot = __classPrivateFieldGet3(this, _BetaMessageStream_currentMessageSnapshot, "f");
      if (event.type === "message_start") {
        if (snapshot) {
          throw new AnthropicError(`Unexpected event order, got ${event.type} before receiving "message_stop"`);
        }
        return event.message;
      }
      if (!snapshot) {
        throw new AnthropicError(`Unexpected event order, got ${event.type} before "message_start"`);
      }
      switch (event.type) {
        case "message_stop":
          return snapshot;
        case "message_delta":
          snapshot.stop_reason = event.delta.stop_reason;
          snapshot.stop_sequence = event.delta.stop_sequence;
          snapshot.usage.output_tokens = event.usage.output_tokens;
          return snapshot;
        case "content_block_start":
          snapshot.content.push(event.content_block);
          return snapshot;
        case "content_block_delta": {
          const snapshotContent = snapshot.content.at(event.index);
          switch (event.delta.type) {
            case "text_delta": {
              if (snapshotContent?.type === "text") {
                snapshotContent.text += event.delta.text;
              }
              break;
            }
            case "citations_delta": {
              if (snapshotContent?.type === "text") {
                snapshotContent.citations ?? (snapshotContent.citations = []);
                snapshotContent.citations.push(event.delta.citation);
              }
              break;
            }
            case "input_json_delta": {
              if (snapshotContent?.type === "tool_use") {
                let jsonBuf = snapshotContent[JSON_BUF_PROPERTY] || "";
                jsonBuf += event.delta.partial_json;
                Object.defineProperty(snapshotContent, JSON_BUF_PROPERTY, {
                  value: jsonBuf,
                  enumerable: false,
                  writable: true
                });
                if (jsonBuf) {
                  snapshotContent.input = partialParse2(jsonBuf);
                }
              }
              break;
            }
            case "thinking_delta": {
              if (snapshotContent?.type === "thinking") {
                snapshotContent.thinking += event.delta.thinking;
              }
              break;
            }
            case "signature_delta": {
              if (snapshotContent?.type === "thinking") {
                snapshotContent.signature = event.delta.signature;
              }
              break;
            }
            default:
              checkNever(event.delta);
          }
          return snapshot;
        }
        case "content_block_stop":
          return snapshot;
      }
    }, Symbol.asyncIterator)]() {
      const pushQueue = [];
      const readQueue = [];
      let done = false;
      this.on("streamEvent", (event) => {
        const reader = readQueue.shift();
        if (reader) {
          reader.resolve(event);
        } else {
          pushQueue.push(event);
        }
      });
      this.on("end", () => {
        done = true;
        for (const reader of readQueue) {
          reader.resolve(void 0);
        }
        readQueue.length = 0;
      });
      this.on("abort", (err) => {
        done = true;
        for (const reader of readQueue) {
          reader.reject(err);
        }
        readQueue.length = 0;
      });
      this.on("error", (err) => {
        done = true;
        for (const reader of readQueue) {
          reader.reject(err);
        }
        readQueue.length = 0;
      });
      return {
        next: async () => {
          if (!pushQueue.length) {
            if (done) {
              return { value: void 0, done: true };
            }
            return new Promise((resolve, reject) => readQueue.push({ resolve, reject })).then((chunk2) => chunk2 ? { value: chunk2, done: false } : { value: void 0, done: true });
          }
          const chunk = pushQueue.shift();
          return { value: chunk, done: false };
        },
        return: async () => {
          this.abort();
          return { value: void 0, done: true };
        }
      };
    }
    toReadableStream() {
      const stream = new Stream(this[Symbol.asyncIterator].bind(this), this.controller);
      return stream.toReadableStream();
    }
  };
  function checkNever(x) {
  }

  // ../../node_modules/@anthropic-ai/sdk/resources/beta/messages/messages.mjs
  var DEPRECATED_MODELS = {
    "claude-1.3": "November 6th, 2024",
    "claude-1.3-100k": "November 6th, 2024",
    "claude-instant-1.1": "November 6th, 2024",
    "claude-instant-1.1-100k": "November 6th, 2024",
    "claude-instant-1.2": "November 6th, 2024",
    "claude-3-sonnet-20240229": "July 21st, 2025",
    "claude-2.1": "July 21st, 2025",
    "claude-2.0": "July 21st, 2025"
  };
  var Messages = class extends APIResource {
    constructor() {
      super(...arguments);
      this.batches = new Batches(this._client);
    }
    create(params, options) {
      const { betas, ...body } = params;
      if (body.model in DEPRECATED_MODELS) {
        console.warn(`The model '${body.model}' is deprecated and will reach end-of-life on ${DEPRECATED_MODELS[body.model]}
Please migrate to a newer model. Visit https://docs.anthropic.com/en/docs/resources/model-deprecations for more information.`);
      }
      return this._client.post("/v1/messages?beta=true", {
        body,
        timeout: this._client._options.timeout ?? (body.stream ? 6e5 : this._client._calculateNonstreamingTimeout(body.max_tokens)),
        ...options,
        headers: {
          ...betas?.toString() != null ? { "anthropic-beta": betas?.toString() } : void 0,
          ...options?.headers
        },
        stream: params.stream ?? false
      });
    }
    /**
     * Create a Message stream
     */
    stream(body, options) {
      return BetaMessageStream.createMessage(this, body, options);
    }
    /**
     * Count the number of tokens in a Message.
     *
     * The Token Count API can be used to count the number of tokens in a Message,
     * including tools, images, and documents, without creating it.
     *
     * Learn more about token counting in our
     * [user guide](/en/docs/build-with-claude/token-counting)
     */
    countTokens(params, options) {
      const { betas, ...body } = params;
      return this._client.post("/v1/messages/count_tokens?beta=true", {
        body,
        ...options,
        headers: {
          "anthropic-beta": [...betas ?? [], "token-counting-2024-11-01"].toString(),
          ...options?.headers
        }
      });
    }
  };
  Messages.Batches = Batches;
  Messages.BetaMessageBatchesPage = BetaMessageBatchesPage;

  // ../../node_modules/@anthropic-ai/sdk/resources/beta/beta.mjs
  var Beta = class extends APIResource {
    constructor() {
      super(...arguments);
      this.models = new Models(this._client);
      this.messages = new Messages(this._client);
    }
  };
  Beta.Models = Models;
  Beta.BetaModelInfosPage = BetaModelInfosPage;
  Beta.Messages = Messages;

  // ../../node_modules/@anthropic-ai/sdk/resources/completions.mjs
  var Completions = class extends APIResource {
    create(body, options) {
      return this._client.post("/v1/complete", {
        body,
        timeout: this._client._options.timeout ?? 6e5,
        ...options,
        stream: body.stream ?? false
      });
    }
  };

  // ../../node_modules/@anthropic-ai/sdk/resources/messages/batches.mjs
  var Batches2 = class extends APIResource {
    /**
     * Send a batch of Message creation requests.
     *
     * The Message Batches API can be used to process multiple Messages API requests at
     * once. Once a Message Batch is created, it begins processing immediately. Batches
     * can take up to 24 hours to complete.
     *
     * Learn more about the Message Batches API in our
     * [user guide](/en/docs/build-with-claude/batch-processing)
     */
    create(body, options) {
      return this._client.post("/v1/messages/batches", { body, ...options });
    }
    /**
     * This endpoint is idempotent and can be used to poll for Message Batch
     * completion. To access the results of a Message Batch, make a request to the
     * `results_url` field in the response.
     *
     * Learn more about the Message Batches API in our
     * [user guide](/en/docs/build-with-claude/batch-processing)
     */
    retrieve(messageBatchId, options) {
      return this._client.get(`/v1/messages/batches/${messageBatchId}`, options);
    }
    list(query = {}, options) {
      if (isRequestOptions(query)) {
        return this.list({}, query);
      }
      return this._client.getAPIList("/v1/messages/batches", MessageBatchesPage, { query, ...options });
    }
    /**
     * Delete a Message Batch.
     *
     * Message Batches can only be deleted once they've finished processing. If you'd
     * like to delete an in-progress batch, you must first cancel it.
     *
     * Learn more about the Message Batches API in our
     * [user guide](/en/docs/build-with-claude/batch-processing)
     */
    delete(messageBatchId, options) {
      return this._client.delete(`/v1/messages/batches/${messageBatchId}`, options);
    }
    /**
     * Batches may be canceled any time before processing ends. Once cancellation is
     * initiated, the batch enters a `canceling` state, at which time the system may
     * complete any in-progress, non-interruptible requests before finalizing
     * cancellation.
     *
     * The number of canceled requests is specified in `request_counts`. To determine
     * which requests were canceled, check the individual results within the batch.
     * Note that cancellation may not result in any canceled requests if they were
     * non-interruptible.
     *
     * Learn more about the Message Batches API in our
     * [user guide](/en/docs/build-with-claude/batch-processing)
     */
    cancel(messageBatchId, options) {
      return this._client.post(`/v1/messages/batches/${messageBatchId}/cancel`, options);
    }
    /**
     * Streams the results of a Message Batch as a `.jsonl` file.
     *
     * Each line in the file is a JSON object containing the result of a single request
     * in the Message Batch. Results are not guaranteed to be in the same order as
     * requests. Use the `custom_id` field to match results to requests.
     *
     * Learn more about the Message Batches API in our
     * [user guide](/en/docs/build-with-claude/batch-processing)
     */
    async results(messageBatchId, options) {
      const batch = await this.retrieve(messageBatchId);
      if (!batch.results_url) {
        throw new AnthropicError(`No batch \`results_url\`; Has it finished processing? ${batch.processing_status} - ${batch.id}`);
      }
      return this._client.get(batch.results_url, {
        ...options,
        headers: {
          Accept: "application/binary",
          ...options?.headers
        },
        __binaryResponse: true
      })._thenUnwrap((_, props) => JSONLDecoder.fromResponse(props.response, props.controller));
    }
  };
  var MessageBatchesPage = class extends Page {
  };
  Batches2.MessageBatchesPage = MessageBatchesPage;

  // ../../node_modules/@anthropic-ai/sdk/lib/MessageStream.mjs
  var __classPrivateFieldSet4 = function(receiver, state, value, kind2, f) {
    if (kind2 === "m") throw new TypeError("Private method is not writable");
    if (kind2 === "a" && !f) throw new TypeError("Private accessor was defined without a setter");
    if (typeof state === "function" ? receiver !== state || !f : !state.has(receiver)) throw new TypeError("Cannot write private member to an object whose class did not declare it");
    return kind2 === "a" ? f.call(receiver, value) : f ? f.value = value : state.set(receiver, value), value;
  };
  var __classPrivateFieldGet4 = function(receiver, state, kind2, f) {
    if (kind2 === "a" && !f) throw new TypeError("Private accessor was defined without a getter");
    if (typeof state === "function" ? receiver !== state || !f : !state.has(receiver)) throw new TypeError("Cannot read private member from an object whose class did not declare it");
    return kind2 === "m" ? f : kind2 === "a" ? f.call(receiver) : f ? f.value : state.get(receiver);
  };
  var _MessageStream_instances;
  var _MessageStream_currentMessageSnapshot;
  var _MessageStream_connectedPromise;
  var _MessageStream_resolveConnectedPromise;
  var _MessageStream_rejectConnectedPromise;
  var _MessageStream_endPromise;
  var _MessageStream_resolveEndPromise;
  var _MessageStream_rejectEndPromise;
  var _MessageStream_listeners;
  var _MessageStream_ended;
  var _MessageStream_errored;
  var _MessageStream_aborted;
  var _MessageStream_catchingPromiseCreated;
  var _MessageStream_response;
  var _MessageStream_request_id;
  var _MessageStream_getFinalMessage;
  var _MessageStream_getFinalText;
  var _MessageStream_handleError;
  var _MessageStream_beginRequest;
  var _MessageStream_addStreamEvent;
  var _MessageStream_endRequest;
  var _MessageStream_accumulateMessage;
  var JSON_BUF_PROPERTY2 = "__json_buf";
  var MessageStream = class _MessageStream {
    constructor() {
      _MessageStream_instances.add(this);
      this.messages = [];
      this.receivedMessages = [];
      _MessageStream_currentMessageSnapshot.set(this, void 0);
      this.controller = new AbortController();
      _MessageStream_connectedPromise.set(this, void 0);
      _MessageStream_resolveConnectedPromise.set(this, () => {
      });
      _MessageStream_rejectConnectedPromise.set(this, () => {
      });
      _MessageStream_endPromise.set(this, void 0);
      _MessageStream_resolveEndPromise.set(this, () => {
      });
      _MessageStream_rejectEndPromise.set(this, () => {
      });
      _MessageStream_listeners.set(this, {});
      _MessageStream_ended.set(this, false);
      _MessageStream_errored.set(this, false);
      _MessageStream_aborted.set(this, false);
      _MessageStream_catchingPromiseCreated.set(this, false);
      _MessageStream_response.set(this, void 0);
      _MessageStream_request_id.set(this, void 0);
      _MessageStream_handleError.set(this, (error) => {
        __classPrivateFieldSet4(this, _MessageStream_errored, true, "f");
        if (error instanceof Error && error.name === "AbortError") {
          error = new APIUserAbortError();
        }
        if (error instanceof APIUserAbortError) {
          __classPrivateFieldSet4(this, _MessageStream_aborted, true, "f");
          return this._emit("abort", error);
        }
        if (error instanceof AnthropicError) {
          return this._emit("error", error);
        }
        if (error instanceof Error) {
          const anthropicError = new AnthropicError(error.message);
          anthropicError.cause = error;
          return this._emit("error", anthropicError);
        }
        return this._emit("error", new AnthropicError(String(error)));
      });
      __classPrivateFieldSet4(this, _MessageStream_connectedPromise, new Promise((resolve, reject) => {
        __classPrivateFieldSet4(this, _MessageStream_resolveConnectedPromise, resolve, "f");
        __classPrivateFieldSet4(this, _MessageStream_rejectConnectedPromise, reject, "f");
      }), "f");
      __classPrivateFieldSet4(this, _MessageStream_endPromise, new Promise((resolve, reject) => {
        __classPrivateFieldSet4(this, _MessageStream_resolveEndPromise, resolve, "f");
        __classPrivateFieldSet4(this, _MessageStream_rejectEndPromise, reject, "f");
      }), "f");
      __classPrivateFieldGet4(this, _MessageStream_connectedPromise, "f").catch(() => {
      });
      __classPrivateFieldGet4(this, _MessageStream_endPromise, "f").catch(() => {
      });
    }
    get response() {
      return __classPrivateFieldGet4(this, _MessageStream_response, "f");
    }
    get request_id() {
      return __classPrivateFieldGet4(this, _MessageStream_request_id, "f");
    }
    /**
     * Returns the `MessageStream` data, the raw `Response` instance and the ID of the request,
     * returned vie the `request-id` header which is useful for debugging requests and resporting
     * issues to Anthropic.
     *
     * This is the same as the `APIPromise.withResponse()` method.
     *
     * This method will raise an error if you created the stream using `MessageStream.fromReadableStream`
     * as no `Response` is available.
     */
    async withResponse() {
      const response = await __classPrivateFieldGet4(this, _MessageStream_connectedPromise, "f");
      if (!response) {
        throw new Error("Could not resolve a `Response` object");
      }
      return {
        data: this,
        response,
        request_id: response.headers.get("request-id")
      };
    }
    /**
     * Intended for use on the frontend, consuming a stream produced with
     * `.toReadableStream()` on the backend.
     *
     * Note that messages sent to the model do not appear in `.on('message')`
     * in this context.
     */
    static fromReadableStream(stream) {
      const runner = new _MessageStream();
      runner._run(() => runner._fromReadableStream(stream));
      return runner;
    }
    static createMessage(messages, params, options) {
      const runner = new _MessageStream();
      for (const message of params.messages) {
        runner._addMessageParam(message);
      }
      runner._run(() => runner._createMessage(messages, { ...params, stream: true }, { ...options, headers: { ...options?.headers, "X-Stainless-Helper-Method": "stream" } }));
      return runner;
    }
    _run(executor) {
      executor().then(() => {
        this._emitFinal();
        this._emit("end");
      }, __classPrivateFieldGet4(this, _MessageStream_handleError, "f"));
    }
    _addMessageParam(message) {
      this.messages.push(message);
    }
    _addMessage(message, emit = true) {
      this.receivedMessages.push(message);
      if (emit) {
        this._emit("message", message);
      }
    }
    async _createMessage(messages, params, options) {
      const signal = options?.signal;
      if (signal) {
        if (signal.aborted)
          this.controller.abort();
        signal.addEventListener("abort", () => this.controller.abort());
      }
      __classPrivateFieldGet4(this, _MessageStream_instances, "m", _MessageStream_beginRequest).call(this);
      const { response, data: stream } = await messages.create({ ...params, stream: true }, { ...options, signal: this.controller.signal }).withResponse();
      this._connected(response);
      for await (const event of stream) {
        __classPrivateFieldGet4(this, _MessageStream_instances, "m", _MessageStream_addStreamEvent).call(this, event);
      }
      if (stream.controller.signal?.aborted) {
        throw new APIUserAbortError();
      }
      __classPrivateFieldGet4(this, _MessageStream_instances, "m", _MessageStream_endRequest).call(this);
    }
    _connected(response) {
      if (this.ended)
        return;
      __classPrivateFieldSet4(this, _MessageStream_response, response, "f");
      __classPrivateFieldSet4(this, _MessageStream_request_id, response?.headers.get("request-id"), "f");
      __classPrivateFieldGet4(this, _MessageStream_resolveConnectedPromise, "f").call(this, response);
      this._emit("connect");
    }
    get ended() {
      return __classPrivateFieldGet4(this, _MessageStream_ended, "f");
    }
    get errored() {
      return __classPrivateFieldGet4(this, _MessageStream_errored, "f");
    }
    get aborted() {
      return __classPrivateFieldGet4(this, _MessageStream_aborted, "f");
    }
    abort() {
      this.controller.abort();
    }
    /**
     * Adds the listener function to the end of the listeners array for the event.
     * No checks are made to see if the listener has already been added. Multiple calls passing
     * the same combination of event and listener will result in the listener being added, and
     * called, multiple times.
     * @returns this MessageStream, so that calls can be chained
     */
    on(event, listener) {
      const listeners = __classPrivateFieldGet4(this, _MessageStream_listeners, "f")[event] || (__classPrivateFieldGet4(this, _MessageStream_listeners, "f")[event] = []);
      listeners.push({ listener });
      return this;
    }
    /**
     * Removes the specified listener from the listener array for the event.
     * off() will remove, at most, one instance of a listener from the listener array. If any single
     * listener has been added multiple times to the listener array for the specified event, then
     * off() must be called multiple times to remove each instance.
     * @returns this MessageStream, so that calls can be chained
     */
    off(event, listener) {
      const listeners = __classPrivateFieldGet4(this, _MessageStream_listeners, "f")[event];
      if (!listeners)
        return this;
      const index = listeners.findIndex((l) => l.listener === listener);
      if (index >= 0)
        listeners.splice(index, 1);
      return this;
    }
    /**
     * Adds a one-time listener function for the event. The next time the event is triggered,
     * this listener is removed and then invoked.
     * @returns this MessageStream, so that calls can be chained
     */
    once(event, listener) {
      const listeners = __classPrivateFieldGet4(this, _MessageStream_listeners, "f")[event] || (__classPrivateFieldGet4(this, _MessageStream_listeners, "f")[event] = []);
      listeners.push({ listener, once: true });
      return this;
    }
    /**
     * This is similar to `.once()`, but returns a Promise that resolves the next time
     * the event is triggered, instead of calling a listener callback.
     * @returns a Promise that resolves the next time given event is triggered,
     * or rejects if an error is emitted.  (If you request the 'error' event,
     * returns a promise that resolves with the error).
     *
     * Example:
     *
     *   const message = await stream.emitted('message') // rejects if the stream errors
     */
    emitted(event) {
      return new Promise((resolve, reject) => {
        __classPrivateFieldSet4(this, _MessageStream_catchingPromiseCreated, true, "f");
        if (event !== "error")
          this.once("error", reject);
        this.once(event, resolve);
      });
    }
    async done() {
      __classPrivateFieldSet4(this, _MessageStream_catchingPromiseCreated, true, "f");
      await __classPrivateFieldGet4(this, _MessageStream_endPromise, "f");
    }
    get currentMessage() {
      return __classPrivateFieldGet4(this, _MessageStream_currentMessageSnapshot, "f");
    }
    /**
     * @returns a promise that resolves with the the final assistant Message response,
     * or rejects if an error occurred or the stream ended prematurely without producing a Message.
     */
    async finalMessage() {
      await this.done();
      return __classPrivateFieldGet4(this, _MessageStream_instances, "m", _MessageStream_getFinalMessage).call(this);
    }
    /**
     * @returns a promise that resolves with the the final assistant Message's text response, concatenated
     * together if there are more than one text blocks.
     * Rejects if an error occurred or the stream ended prematurely without producing a Message.
     */
    async finalText() {
      await this.done();
      return __classPrivateFieldGet4(this, _MessageStream_instances, "m", _MessageStream_getFinalText).call(this);
    }
    _emit(event, ...args) {
      if (__classPrivateFieldGet4(this, _MessageStream_ended, "f"))
        return;
      if (event === "end") {
        __classPrivateFieldSet4(this, _MessageStream_ended, true, "f");
        __classPrivateFieldGet4(this, _MessageStream_resolveEndPromise, "f").call(this);
      }
      const listeners = __classPrivateFieldGet4(this, _MessageStream_listeners, "f")[event];
      if (listeners) {
        __classPrivateFieldGet4(this, _MessageStream_listeners, "f")[event] = listeners.filter((l) => !l.once);
        listeners.forEach(({ listener }) => listener(...args));
      }
      if (event === "abort") {
        const error = args[0];
        if (!__classPrivateFieldGet4(this, _MessageStream_catchingPromiseCreated, "f") && !listeners?.length) {
          Promise.reject(error);
        }
        __classPrivateFieldGet4(this, _MessageStream_rejectConnectedPromise, "f").call(this, error);
        __classPrivateFieldGet4(this, _MessageStream_rejectEndPromise, "f").call(this, error);
        this._emit("end");
        return;
      }
      if (event === "error") {
        const error = args[0];
        if (!__classPrivateFieldGet4(this, _MessageStream_catchingPromiseCreated, "f") && !listeners?.length) {
          Promise.reject(error);
        }
        __classPrivateFieldGet4(this, _MessageStream_rejectConnectedPromise, "f").call(this, error);
        __classPrivateFieldGet4(this, _MessageStream_rejectEndPromise, "f").call(this, error);
        this._emit("end");
      }
    }
    _emitFinal() {
      const finalMessage = this.receivedMessages.at(-1);
      if (finalMessage) {
        this._emit("finalMessage", __classPrivateFieldGet4(this, _MessageStream_instances, "m", _MessageStream_getFinalMessage).call(this));
      }
    }
    async _fromReadableStream(readableStream, options) {
      const signal = options?.signal;
      if (signal) {
        if (signal.aborted)
          this.controller.abort();
        signal.addEventListener("abort", () => this.controller.abort());
      }
      __classPrivateFieldGet4(this, _MessageStream_instances, "m", _MessageStream_beginRequest).call(this);
      this._connected(null);
      const stream = Stream.fromReadableStream(readableStream, this.controller);
      for await (const event of stream) {
        __classPrivateFieldGet4(this, _MessageStream_instances, "m", _MessageStream_addStreamEvent).call(this, event);
      }
      if (stream.controller.signal?.aborted) {
        throw new APIUserAbortError();
      }
      __classPrivateFieldGet4(this, _MessageStream_instances, "m", _MessageStream_endRequest).call(this);
    }
    [(_MessageStream_currentMessageSnapshot = /* @__PURE__ */ new WeakMap(), _MessageStream_connectedPromise = /* @__PURE__ */ new WeakMap(), _MessageStream_resolveConnectedPromise = /* @__PURE__ */ new WeakMap(), _MessageStream_rejectConnectedPromise = /* @__PURE__ */ new WeakMap(), _MessageStream_endPromise = /* @__PURE__ */ new WeakMap(), _MessageStream_resolveEndPromise = /* @__PURE__ */ new WeakMap(), _MessageStream_rejectEndPromise = /* @__PURE__ */ new WeakMap(), _MessageStream_listeners = /* @__PURE__ */ new WeakMap(), _MessageStream_ended = /* @__PURE__ */ new WeakMap(), _MessageStream_errored = /* @__PURE__ */ new WeakMap(), _MessageStream_aborted = /* @__PURE__ */ new WeakMap(), _MessageStream_catchingPromiseCreated = /* @__PURE__ */ new WeakMap(), _MessageStream_response = /* @__PURE__ */ new WeakMap(), _MessageStream_request_id = /* @__PURE__ */ new WeakMap(), _MessageStream_handleError = /* @__PURE__ */ new WeakMap(), _MessageStream_instances = /* @__PURE__ */ new WeakSet(), _MessageStream_getFinalMessage = function _MessageStream_getFinalMessage2() {
      if (this.receivedMessages.length === 0) {
        throw new AnthropicError("stream ended without producing a Message with role=assistant");
      }
      return this.receivedMessages.at(-1);
    }, _MessageStream_getFinalText = function _MessageStream_getFinalText2() {
      if (this.receivedMessages.length === 0) {
        throw new AnthropicError("stream ended without producing a Message with role=assistant");
      }
      const textBlocks = this.receivedMessages.at(-1).content.filter((block) => block.type === "text").map((block) => block.text);
      if (textBlocks.length === 0) {
        throw new AnthropicError("stream ended without producing a content block with type=text");
      }
      return textBlocks.join(" ");
    }, _MessageStream_beginRequest = function _MessageStream_beginRequest2() {
      if (this.ended)
        return;
      __classPrivateFieldSet4(this, _MessageStream_currentMessageSnapshot, void 0, "f");
    }, _MessageStream_addStreamEvent = function _MessageStream_addStreamEvent2(event) {
      if (this.ended)
        return;
      const messageSnapshot = __classPrivateFieldGet4(this, _MessageStream_instances, "m", _MessageStream_accumulateMessage).call(this, event);
      this._emit("streamEvent", event, messageSnapshot);
      switch (event.type) {
        case "content_block_delta": {
          const content = messageSnapshot.content.at(-1);
          switch (event.delta.type) {
            case "text_delta": {
              if (content.type === "text") {
                this._emit("text", event.delta.text, content.text || "");
              }
              break;
            }
            case "citations_delta": {
              if (content.type === "text") {
                this._emit("citation", event.delta.citation, content.citations ?? []);
              }
              break;
            }
            case "input_json_delta": {
              if (content.type === "tool_use" && content.input) {
                this._emit("inputJson", event.delta.partial_json, content.input);
              }
              break;
            }
            case "thinking_delta": {
              if (content.type === "thinking") {
                this._emit("thinking", event.delta.thinking, content.thinking);
              }
              break;
            }
            case "signature_delta": {
              if (content.type === "thinking") {
                this._emit("signature", content.signature);
              }
              break;
            }
            default:
              checkNever2(event.delta);
          }
          break;
        }
        case "message_stop": {
          this._addMessageParam(messageSnapshot);
          this._addMessage(messageSnapshot, true);
          break;
        }
        case "content_block_stop": {
          this._emit("contentBlock", messageSnapshot.content.at(-1));
          break;
        }
        case "message_start": {
          __classPrivateFieldSet4(this, _MessageStream_currentMessageSnapshot, messageSnapshot, "f");
          break;
        }
        case "content_block_start":
        case "message_delta":
          break;
      }
    }, _MessageStream_endRequest = function _MessageStream_endRequest2() {
      if (this.ended) {
        throw new AnthropicError(`stream has ended, this shouldn't happen`);
      }
      const snapshot = __classPrivateFieldGet4(this, _MessageStream_currentMessageSnapshot, "f");
      if (!snapshot) {
        throw new AnthropicError(`request ended without sending any chunks`);
      }
      __classPrivateFieldSet4(this, _MessageStream_currentMessageSnapshot, void 0, "f");
      return snapshot;
    }, _MessageStream_accumulateMessage = function _MessageStream_accumulateMessage2(event) {
      let snapshot = __classPrivateFieldGet4(this, _MessageStream_currentMessageSnapshot, "f");
      if (event.type === "message_start") {
        if (snapshot) {
          throw new AnthropicError(`Unexpected event order, got ${event.type} before receiving "message_stop"`);
        }
        return event.message;
      }
      if (!snapshot) {
        throw new AnthropicError(`Unexpected event order, got ${event.type} before "message_start"`);
      }
      switch (event.type) {
        case "message_stop":
          return snapshot;
        case "message_delta":
          snapshot.stop_reason = event.delta.stop_reason;
          snapshot.stop_sequence = event.delta.stop_sequence;
          snapshot.usage.output_tokens = event.usage.output_tokens;
          return snapshot;
        case "content_block_start":
          snapshot.content.push(event.content_block);
          return snapshot;
        case "content_block_delta": {
          const snapshotContent = snapshot.content.at(event.index);
          switch (event.delta.type) {
            case "text_delta": {
              if (snapshotContent?.type === "text") {
                snapshotContent.text += event.delta.text;
              }
              break;
            }
            case "citations_delta": {
              if (snapshotContent?.type === "text") {
                snapshotContent.citations ?? (snapshotContent.citations = []);
                snapshotContent.citations.push(event.delta.citation);
              }
              break;
            }
            case "input_json_delta": {
              if (snapshotContent?.type === "tool_use") {
                let jsonBuf = snapshotContent[JSON_BUF_PROPERTY2] || "";
                jsonBuf += event.delta.partial_json;
                Object.defineProperty(snapshotContent, JSON_BUF_PROPERTY2, {
                  value: jsonBuf,
                  enumerable: false,
                  writable: true
                });
                if (jsonBuf) {
                  snapshotContent.input = partialParse2(jsonBuf);
                }
              }
              break;
            }
            case "thinking_delta": {
              if (snapshotContent?.type === "thinking") {
                snapshotContent.thinking += event.delta.thinking;
              }
              break;
            }
            case "signature_delta": {
              if (snapshotContent?.type === "thinking") {
                snapshotContent.signature = event.delta.signature;
              }
              break;
            }
            default:
              checkNever2(event.delta);
          }
          return snapshot;
        }
        case "content_block_stop":
          return snapshot;
      }
    }, Symbol.asyncIterator)]() {
      const pushQueue = [];
      const readQueue = [];
      let done = false;
      this.on("streamEvent", (event) => {
        const reader = readQueue.shift();
        if (reader) {
          reader.resolve(event);
        } else {
          pushQueue.push(event);
        }
      });
      this.on("end", () => {
        done = true;
        for (const reader of readQueue) {
          reader.resolve(void 0);
        }
        readQueue.length = 0;
      });
      this.on("abort", (err) => {
        done = true;
        for (const reader of readQueue) {
          reader.reject(err);
        }
        readQueue.length = 0;
      });
      this.on("error", (err) => {
        done = true;
        for (const reader of readQueue) {
          reader.reject(err);
        }
        readQueue.length = 0;
      });
      return {
        next: async () => {
          if (!pushQueue.length) {
            if (done) {
              return { value: void 0, done: true };
            }
            return new Promise((resolve, reject) => readQueue.push({ resolve, reject })).then((chunk2) => chunk2 ? { value: chunk2, done: false } : { value: void 0, done: true });
          }
          const chunk = pushQueue.shift();
          return { value: chunk, done: false };
        },
        return: async () => {
          this.abort();
          return { value: void 0, done: true };
        }
      };
    }
    toReadableStream() {
      const stream = new Stream(this[Symbol.asyncIterator].bind(this), this.controller);
      return stream.toReadableStream();
    }
  };
  function checkNever2(x) {
  }

  // ../../node_modules/@anthropic-ai/sdk/resources/messages/messages.mjs
  var Messages2 = class extends APIResource {
    constructor() {
      super(...arguments);
      this.batches = new Batches2(this._client);
    }
    create(body, options) {
      if (body.model in DEPRECATED_MODELS2) {
        console.warn(`The model '${body.model}' is deprecated and will reach end-of-life on ${DEPRECATED_MODELS2[body.model]}
Please migrate to a newer model. Visit https://docs.anthropic.com/en/docs/resources/model-deprecations for more information.`);
      }
      return this._client.post("/v1/messages", {
        body,
        timeout: this._client._options.timeout ?? (body.stream ? 6e5 : this._client._calculateNonstreamingTimeout(body.max_tokens)),
        ...options,
        stream: body.stream ?? false
      });
    }
    /**
     * Create a Message stream
     */
    stream(body, options) {
      return MessageStream.createMessage(this, body, options);
    }
    /**
     * Count the number of tokens in a Message.
     *
     * The Token Count API can be used to count the number of tokens in a Message,
     * including tools, images, and documents, without creating it.
     *
     * Learn more about token counting in our
     * [user guide](/en/docs/build-with-claude/token-counting)
     */
    countTokens(body, options) {
      return this._client.post("/v1/messages/count_tokens", { body, ...options });
    }
  };
  var DEPRECATED_MODELS2 = {
    "claude-1.3": "November 6th, 2024",
    "claude-1.3-100k": "November 6th, 2024",
    "claude-instant-1.1": "November 6th, 2024",
    "claude-instant-1.1-100k": "November 6th, 2024",
    "claude-instant-1.2": "November 6th, 2024",
    "claude-3-sonnet-20240229": "July 21st, 2025",
    "claude-2.1": "July 21st, 2025",
    "claude-2.0": "July 21st, 2025"
  };
  Messages2.Batches = Batches2;
  Messages2.MessageBatchesPage = MessageBatchesPage;

  // ../../node_modules/@anthropic-ai/sdk/resources/models.mjs
  var Models2 = class extends APIResource {
    /**
     * Get a specific model.
     *
     * The Models API response can be used to determine information about a specific
     * model or resolve a model alias to a model ID.
     */
    retrieve(modelId, options) {
      return this._client.get(`/v1/models/${modelId}`, options);
    }
    list(query = {}, options) {
      if (isRequestOptions(query)) {
        return this.list({}, query);
      }
      return this._client.getAPIList("/v1/models", ModelInfosPage, { query, ...options });
    }
  };
  var ModelInfosPage = class extends Page {
  };
  Models2.ModelInfosPage = ModelInfosPage;

  // ../../node_modules/@anthropic-ai/sdk/index.mjs
  var _a;
  var Anthropic = class extends APIClient {
    /**
     * API Client for interfacing with the Anthropic API.
     *
     * @param {string | null | undefined} [opts.apiKey=process.env['ANTHROPIC_API_KEY'] ?? null]
     * @param {string | null | undefined} [opts.authToken=process.env['ANTHROPIC_AUTH_TOKEN'] ?? null]
     * @param {string} [opts.baseURL=process.env['ANTHROPIC_BASE_URL'] ?? https://api.anthropic.com] - Override the default base URL for the API.
     * @param {number} [opts.timeout=10 minutes] - The maximum amount of time (in milliseconds) the client will wait for a response before timing out.
     * @param {number} [opts.httpAgent] - An HTTP agent used to manage HTTP(s) connections.
     * @param {Core.Fetch} [opts.fetch] - Specify a custom `fetch` function implementation.
     * @param {number} [opts.maxRetries=2] - The maximum number of times the client will retry a request.
     * @param {Core.Headers} opts.defaultHeaders - Default headers to include with every request to the API.
     * @param {Core.DefaultQuery} opts.defaultQuery - Default query parameters to include with every request to the API.
     * @param {boolean} [opts.dangerouslyAllowBrowser=false] - By default, client-side use of this library is not allowed, as it risks exposing your secret API credentials to attackers.
     */
    constructor({ baseURL = readEnv("ANTHROPIC_BASE_URL"), apiKey = readEnv("ANTHROPIC_API_KEY") ?? null, authToken = readEnv("ANTHROPIC_AUTH_TOKEN") ?? null, ...opts } = {}) {
      const options = {
        apiKey,
        authToken,
        ...opts,
        baseURL: baseURL || `https://api.anthropic.com`
      };
      if (!options.dangerouslyAllowBrowser && isRunningInBrowser()) {
        throw new AnthropicError("It looks like you're running in a browser-like environment.\n\nThis is disabled by default, as it risks exposing your secret API credentials to attackers.\nIf you understand the risks and have appropriate mitigations in place,\nyou can set the `dangerouslyAllowBrowser` option to `true`, e.g.,\n\nnew Anthropic({ apiKey, dangerouslyAllowBrowser: true });\n");
      }
      super({
        baseURL: options.baseURL,
        timeout: options.timeout ?? 6e5,
        httpAgent: options.httpAgent,
        maxRetries: options.maxRetries,
        fetch: options.fetch
      });
      this.completions = new Completions(this);
      this.messages = new Messages2(this);
      this.models = new Models2(this);
      this.beta = new Beta(this);
      this._options = options;
      this.apiKey = apiKey;
      this.authToken = authToken;
    }
    defaultQuery() {
      return this._options.defaultQuery;
    }
    defaultHeaders(opts) {
      return {
        ...super.defaultHeaders(opts),
        ...this._options.dangerouslyAllowBrowser ? { "anthropic-dangerous-direct-browser-access": "true" } : void 0,
        "anthropic-version": "2023-06-01",
        ...this._options.defaultHeaders
      };
    }
    validateHeaders(headers, customHeaders) {
      if (this.apiKey && headers["x-api-key"]) {
        return;
      }
      if (customHeaders["x-api-key"] === null) {
        return;
      }
      if (this.authToken && headers["authorization"]) {
        return;
      }
      if (customHeaders["authorization"] === null) {
        return;
      }
      throw new Error('Could not resolve authentication method. Expected either apiKey or authToken to be set. Or for one of the "X-Api-Key" or "Authorization" headers to be explicitly omitted');
    }
    authHeaders(opts) {
      const apiKeyAuth = this.apiKeyAuth(opts);
      const bearerAuth = this.bearerAuth(opts);
      if (apiKeyAuth != null && !isEmptyObj(apiKeyAuth)) {
        return apiKeyAuth;
      }
      if (bearerAuth != null && !isEmptyObj(bearerAuth)) {
        return bearerAuth;
      }
      return {};
    }
    apiKeyAuth(opts) {
      if (this.apiKey == null) {
        return {};
      }
      return { "X-Api-Key": this.apiKey };
    }
    bearerAuth(opts) {
      if (this.authToken == null) {
        return {};
      }
      return { Authorization: `Bearer ${this.authToken}` };
    }
  };
  _a = Anthropic;
  Anthropic.Anthropic = _a;
  Anthropic.HUMAN_PROMPT = "\n\nHuman:";
  Anthropic.AI_PROMPT = "\n\nAssistant:";
  Anthropic.DEFAULT_TIMEOUT = 6e5;
  Anthropic.AnthropicError = AnthropicError;
  Anthropic.APIError = APIError;
  Anthropic.APIConnectionError = APIConnectionError;
  Anthropic.APIConnectionTimeoutError = APIConnectionTimeoutError;
  Anthropic.APIUserAbortError = APIUserAbortError;
  Anthropic.NotFoundError = NotFoundError;
  Anthropic.ConflictError = ConflictError;
  Anthropic.RateLimitError = RateLimitError;
  Anthropic.BadRequestError = BadRequestError;
  Anthropic.AuthenticationError = AuthenticationError;
  Anthropic.InternalServerError = InternalServerError;
  Anthropic.PermissionDeniedError = PermissionDeniedError;
  Anthropic.UnprocessableEntityError = UnprocessableEntityError;
  Anthropic.toFile = toFile;
  Anthropic.fileFromPath = fileFromPath;
  Anthropic.Completions = Completions;
  Anthropic.Messages = Messages2;
  Anthropic.Models = Models2;
  Anthropic.ModelInfosPage = ModelInfosPage;
  Anthropic.Beta = Beta;
  var { HUMAN_PROMPT, AI_PROMPT } = Anthropic;
  var sdk_default = Anthropic;

  // ../../packages/ai/dist/providers/anthropic.js
  function sanitizeSurrogates(text) {
    return text.replace(/[\uD800-\uDFFF]/g, "");
  }
  function normalizeToolCallId(id) {
    return id.replace(/[^a-zA-Z0-9_-]/g, "_").slice(0, 64);
  }
  function mapStopReason2(reason) {
    switch (reason) {
      case "end_turn":
        return "stop";
      case "max_tokens":
        return "length";
      case "tool_use":
        return "toolUse";
      default:
        return "stop";
    }
  }
  function convertContentBlocks(content) {
    const supportedBlocks = content.filter((block) => block.type === "text" || block.type === "image");
    const hasImages = supportedBlocks.some((c) => c.type === "image");
    if (!hasImages) {
      const text = supportedBlocks.filter((c) => c.type === "text").map((c) => c.text).join("\n");
      return sanitizeSurrogates(text || "(unsupported tool result omitted)");
    }
    const blocks = supportedBlocks.map((block) => {
      if (block.type === "text") {
        return { type: "text", text: sanitizeSurrogates(block.text) };
      }
      return {
        type: "image",
        source: {
          type: "base64",
          media_type: block.mimeType,
          data: block.data
        }
      };
    });
    const hasText = blocks.some((b) => b.type === "text");
    if (!hasText) {
      blocks.unshift({ type: "text", text: "(see attached image)" });
    }
    return blocks;
  }
  function convertUserContentBlocks(content) {
    const blocks = [];
    for (const item of content) {
      if (item.type === "text") {
        blocks.push({ type: "text", text: sanitizeSurrogates(item.text) });
        continue;
      }
      if (item.type === "image") {
        blocks.push({
          type: "image",
          source: {
            type: "base64",
            media_type: item.mimeType,
            data: item.data
          }
        });
        continue;
      }
      if (item.type === "video") {
        const video = item;
        blocks.push({
          type: "text",
          text: sanitizeSurrogates(`(video attachment omitted for anthropic provider: ${video.mimeType})`)
        });
        continue;
      }
      if (item.type === "fileRef") {
        const ref = item;
        blocks.push({
          type: "text",
          text: sanitizeSurrogates(`(file reference omitted for anthropic provider: ${ref.url})`)
        });
      }
    }
    return blocks;
  }
  function convertMessages2(messages, model) {
    const params = [];
    const transformedMessages = transformMessages(messages, model, normalizeToolCallId);
    for (let i = 0; i < transformedMessages.length; i++) {
      const msg = transformedMessages[i];
      if (msg.role === "user") {
        if (typeof msg.content === "string") {
          if (msg.content.trim().length > 0) {
            params.push({ role: "user", content: sanitizeSurrogates(msg.content) });
          }
        } else {
          const blocks = convertUserContentBlocks(msg.content);
          let filteredBlocks = !model?.input.includes("image") ? blocks.filter((b) => b.type !== "image") : blocks;
          filteredBlocks = filteredBlocks.filter((b) => b.type === "text" ? b.text.trim().length > 0 : true);
          if (filteredBlocks.length === 0)
            continue;
          params.push({ role: "user", content: filteredBlocks });
        }
      } else if (msg.role === "assistant") {
        const blocks = [];
        for (const block of msg.content) {
          if (block.type === "text") {
            if (block.text.trim().length === 0)
              continue;
            blocks.push({ type: "text", text: sanitizeSurrogates(block.text) });
          } else if (block.type === "thinking") {
            if (block.thinking.trim().length === 0)
              continue;
            if (!block.thinkingSignature || block.thinkingSignature.trim().length === 0) {
              blocks.push({ type: "text", text: sanitizeSurrogates(block.thinking) });
            } else {
              blocks.push({
                type: "thinking",
                thinking: sanitizeSurrogates(block.thinking),
                signature: block.thinkingSignature
              });
            }
          } else if (block.type === "toolCall") {
            blocks.push({ type: "tool_use", id: block.id, name: block.name, input: block.arguments ?? {} });
          }
        }
        if (blocks.length === 0)
          continue;
        params.push({ role: "assistant", content: blocks });
      } else if (msg.role === "toolResult") {
        const toolResults = [];
        toolResults.push({
          type: "tool_result",
          tool_use_id: msg.toolCallId,
          content: convertContentBlocks(msg.content),
          is_error: msg.isError
        });
        let j = i + 1;
        while (j < transformedMessages.length && transformedMessages[j].role === "toolResult") {
          const nextMsg = transformedMessages[j];
          toolResults.push({
            type: "tool_result",
            tool_use_id: nextMsg.toolCallId,
            content: convertContentBlocks(nextMsg.content),
            is_error: nextMsg.isError
          });
          j++;
        }
        i = j - 1;
        params.push({ role: "user", content: toolResults });
      }
    }
    return params;
  }
  function convertTools2(tools) {
    if (!tools)
      return [];
    return tools.map((tool) => {
      const jsonSchema = tool.parameters;
      return {
        name: tool.name,
        description: tool.description,
        input_schema: {
          type: "object",
          properties: jsonSchema.properties || {},
          required: jsonSchema.required || []
        }
      };
    });
  }
  var streamAnthropic = (model, context, options) => {
    const stream = new AssistantMessageEventStream();
    (async () => {
      const output = {
        role: "assistant",
        content: [],
        api: model.api,
        provider: model.provider,
        model: model.id,
        usage: {
          input: 0,
          output: 0,
          cacheRead: 0,
          cacheWrite: 0,
          totalTokens: 0,
          cost: { input: 0, output: 0, cacheRead: 0, cacheWrite: 0, total: 0 }
        },
        stopReason: "stop",
        timestamp: Date.now()
      };
      try {
        const apiKey = options?.apiKey ?? getEnvApiKey(model.provider) ?? "";
        const client = new sdk_default({
          apiKey,
          baseURL: model.baseUrl,
          dangerouslyAllowBrowser: true,
          defaultHeaders: {
            accept: "application/json",
            "anthropic-dangerous-direct-browser-access": "true",
            "anthropic-version": "2023-06-01",
            ...model.headers || {}
          }
        });
        const params = {
          model: model.id,
          messages: convertMessages2(context.messages, model),
          max_tokens: options?.maxTokens || Math.floor(model.maxTokens / 3) || 1024,
          stream: true
        };
        if (context.systemPrompt) {
          params.system = [{ type: "text", text: sanitizeSurrogates(context.systemPrompt) }];
        }
        if (options?.temperature !== void 0) {
          params.temperature = options.temperature;
        }
        if (context.tools?.length) {
          params.tools = convertTools2(context.tools);
        }
        if (options?.thinkingEnabled && model.reasoning) {
          params.thinking = {
            type: "enabled",
            budget_tokens: options.thinkingBudgetTokens || 1024
          };
        }
        if (options?.toolChoice) {
          if (typeof options.toolChoice === "string") {
            params.tool_choice = { type: options.toolChoice };
          } else {
            params.tool_choice = options.toolChoice;
          }
        }
        options?.onPayload?.(params);
        const anthropicStream = client.messages.stream({ ...params, stream: true }, { signal: options?.signal });
        stream.push({ type: "start", partial: output });
        const blocks = output.content;
        for await (const event of anthropicStream) {
          if (event.type === "message_start") {
            output.usage.input = event.message.usage.input_tokens || 0;
            output.usage.output = event.message.usage.output_tokens || 0;
            output.usage.cacheRead = event.message.usage.cache_read_input_tokens || 0;
            output.usage.cacheWrite = event.message.usage.cache_creation_input_tokens || 0;
            output.usage.totalTokens = output.usage.input + output.usage.output + output.usage.cacheRead + output.usage.cacheWrite;
            calculateCost(model, output.usage);
          } else if (event.type === "content_block_start") {
            if (event.content_block.type === "text") {
              const block = { type: "text", text: "", index: event.index };
              output.content.push(block);
              stream.push({ type: "text_start", contentIndex: output.content.length - 1, partial: output });
            } else if (event.content_block.type === "thinking") {
              const block = { type: "thinking", thinking: "", thinkingSignature: "", index: event.index };
              output.content.push(block);
              stream.push({
                type: "thinking_start",
                contentIndex: output.content.length - 1,
                partial: output
              });
            } else if (event.content_block.type === "tool_use") {
              const block = {
                type: "toolCall",
                id: event.content_block.id,
                name: event.content_block.name,
                arguments: event.content_block.input ?? {},
                partialJson: "",
                index: event.index
              };
              output.content.push(block);
              stream.push({
                type: "toolcall_start",
                contentIndex: output.content.length - 1,
                partial: output
              });
            }
          } else if (event.type === "content_block_delta") {
            if (event.delta.type === "text_delta") {
              const index = blocks.findIndex((b) => b.index === event.index);
              const block = blocks[index];
              if (block?.type === "text") {
                block.text += event.delta.text;
                stream.push({ type: "text_delta", contentIndex: index, delta: event.delta.text, partial: output });
              }
            } else if (event.delta.type === "thinking_delta") {
              const index = blocks.findIndex((b) => b.index === event.index);
              const block = blocks[index];
              if (block?.type === "thinking") {
                block.thinking += event.delta.thinking;
                stream.push({
                  type: "thinking_delta",
                  contentIndex: index,
                  delta: event.delta.thinking,
                  partial: output
                });
              }
            } else if (event.delta.type === "input_json_delta") {
              const index = blocks.findIndex((b) => b.index === event.index);
              const block = blocks[index];
              if (block?.type === "toolCall") {
                block.partialJson += event.delta.partial_json;
                block.arguments = parseStreamingJson(block.partialJson);
                stream.push({
                  type: "toolcall_delta",
                  contentIndex: index,
                  delta: event.delta.partial_json,
                  partial: output
                });
              }
            } else if (event.delta.type === "signature_delta") {
              const index = blocks.findIndex((b) => b.index === event.index);
              const block = blocks[index];
              if (block?.type === "thinking") {
                block.thinkingSignature = (block.thinkingSignature || "") + event.delta.signature;
              }
            }
          } else if (event.type === "content_block_stop") {
            const index = blocks.findIndex((b) => b.index === event.index);
            const block = blocks[index];
            if (block) {
              delete block.index;
              if (block.type === "text") {
                stream.push({
                  type: "text_end",
                  contentIndex: index,
                  content: block.text,
                  partial: output
                });
              } else if (block.type === "thinking") {
                stream.push({
                  type: "thinking_end",
                  contentIndex: index,
                  content: block.thinking,
                  partial: output
                });
              } else if (block.type === "toolCall") {
                block.arguments = parseStreamingJson(block.partialJson);
                delete block.partialJson;
                stream.push({
                  type: "toolcall_end",
                  contentIndex: index,
                  toolCall: block,
                  partial: output
                });
              }
            }
          } else if (event.type === "message_delta") {
            if (event.delta.stop_reason) {
              output.stopReason = mapStopReason2(event.delta.stop_reason);
            }
            if (event.usage.input_tokens != null)
              output.usage.input = event.usage.input_tokens;
            if (event.usage.output_tokens != null)
              output.usage.output = event.usage.output_tokens;
            if (event.usage.cache_read_input_tokens != null)
              output.usage.cacheRead = event.usage.cache_read_input_tokens;
            if (event.usage.cache_creation_input_tokens != null)
              output.usage.cacheWrite = event.usage.cache_creation_input_tokens;
            output.usage.totalTokens = output.usage.input + output.usage.output + output.usage.cacheRead + output.usage.cacheWrite;
            calculateCost(model, output.usage);
          }
        }
        if (options?.signal?.aborted) {
          throw new Error("Request was aborted");
        }
        stream.push({ type: "done", reason: output.stopReason, message: output });
        stream.end();
      } catch (error) {
        for (const block of output.content)
          delete block.index;
        output.stopReason = options?.signal?.aborted ? "aborted" : "error";
        output.errorMessage = error instanceof Error ? error.message : JSON.stringify(error);
        stream.push({ type: "error", reason: output.stopReason, error: output });
        stream.end();
      }
    })();
    return stream;
  };
  var streamSimpleAnthropic = (model, context, options) => {
    const apiKey = options?.apiKey || getEnvApiKey(model.provider);
    if (!apiKey) {
      throw new Error(`No API key for provider: ${model.provider}`);
    }
    const base = buildBaseOptions(model, options, apiKey);
    if (!options?.reasoning) {
      return streamAnthropic(model, context, { ...base, thinkingEnabled: false });
    }
    const adjusted = adjustMaxTokensForThinking(base.maxTokens || 0, model.maxTokens, options.reasoning, options.thinkingBudgets);
    return streamAnthropic(model, context, {
      ...base,
      maxTokens: adjusted.maxTokens,
      thinkingEnabled: true,
      thinkingBudgetTokens: adjusted.thinkingBudget
    });
  };

  // ../../packages/ai/dist/stream.js
  function resolveApiProvider(api) {
    const provider = getApiProvider(api);
    if (!provider) {
      throw new Error(`No API provider registered for api: ${api}`);
    }
    return provider;
  }
  function streamSimple(model, context, options) {
    const provider = resolveApiProvider(model.api);
    return provider.streamSimple(model, context, options);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/errors/function.mjs
  function DefaultErrorFunction(error) {
    switch (error.errorType) {
      case ValueErrorType.ArrayContains:
        return "Expected array to contain at least one matching value";
      case ValueErrorType.ArrayMaxContains:
        return `Expected array to contain no more than ${error.schema.maxContains} matching values`;
      case ValueErrorType.ArrayMinContains:
        return `Expected array to contain at least ${error.schema.minContains} matching values`;
      case ValueErrorType.ArrayMaxItems:
        return `Expected array length to be less or equal to ${error.schema.maxItems}`;
      case ValueErrorType.ArrayMinItems:
        return `Expected array length to be greater or equal to ${error.schema.minItems}`;
      case ValueErrorType.ArrayUniqueItems:
        return "Expected array elements to be unique";
      case ValueErrorType.Array:
        return "Expected array";
      case ValueErrorType.AsyncIterator:
        return "Expected AsyncIterator";
      case ValueErrorType.BigIntExclusiveMaximum:
        return `Expected bigint to be less than ${error.schema.exclusiveMaximum}`;
      case ValueErrorType.BigIntExclusiveMinimum:
        return `Expected bigint to be greater than ${error.schema.exclusiveMinimum}`;
      case ValueErrorType.BigIntMaximum:
        return `Expected bigint to be less or equal to ${error.schema.maximum}`;
      case ValueErrorType.BigIntMinimum:
        return `Expected bigint to be greater or equal to ${error.schema.minimum}`;
      case ValueErrorType.BigIntMultipleOf:
        return `Expected bigint to be a multiple of ${error.schema.multipleOf}`;
      case ValueErrorType.BigInt:
        return "Expected bigint";
      case ValueErrorType.Boolean:
        return "Expected boolean";
      case ValueErrorType.DateExclusiveMinimumTimestamp:
        return `Expected Date timestamp to be greater than ${error.schema.exclusiveMinimumTimestamp}`;
      case ValueErrorType.DateExclusiveMaximumTimestamp:
        return `Expected Date timestamp to be less than ${error.schema.exclusiveMaximumTimestamp}`;
      case ValueErrorType.DateMinimumTimestamp:
        return `Expected Date timestamp to be greater or equal to ${error.schema.minimumTimestamp}`;
      case ValueErrorType.DateMaximumTimestamp:
        return `Expected Date timestamp to be less or equal to ${error.schema.maximumTimestamp}`;
      case ValueErrorType.DateMultipleOfTimestamp:
        return `Expected Date timestamp to be a multiple of ${error.schema.multipleOfTimestamp}`;
      case ValueErrorType.Date:
        return "Expected Date";
      case ValueErrorType.Function:
        return "Expected function";
      case ValueErrorType.IntegerExclusiveMaximum:
        return `Expected integer to be less than ${error.schema.exclusiveMaximum}`;
      case ValueErrorType.IntegerExclusiveMinimum:
        return `Expected integer to be greater than ${error.schema.exclusiveMinimum}`;
      case ValueErrorType.IntegerMaximum:
        return `Expected integer to be less or equal to ${error.schema.maximum}`;
      case ValueErrorType.IntegerMinimum:
        return `Expected integer to be greater or equal to ${error.schema.minimum}`;
      case ValueErrorType.IntegerMultipleOf:
        return `Expected integer to be a multiple of ${error.schema.multipleOf}`;
      case ValueErrorType.Integer:
        return "Expected integer";
      case ValueErrorType.IntersectUnevaluatedProperties:
        return "Unexpected property";
      case ValueErrorType.Intersect:
        return "Expected all values to match";
      case ValueErrorType.Iterator:
        return "Expected Iterator";
      case ValueErrorType.Literal:
        return `Expected ${typeof error.schema.const === "string" ? `'${error.schema.const}'` : error.schema.const}`;
      case ValueErrorType.Never:
        return "Never";
      case ValueErrorType.Not:
        return "Value should not match";
      case ValueErrorType.Null:
        return "Expected null";
      case ValueErrorType.NumberExclusiveMaximum:
        return `Expected number to be less than ${error.schema.exclusiveMaximum}`;
      case ValueErrorType.NumberExclusiveMinimum:
        return `Expected number to be greater than ${error.schema.exclusiveMinimum}`;
      case ValueErrorType.NumberMaximum:
        return `Expected number to be less or equal to ${error.schema.maximum}`;
      case ValueErrorType.NumberMinimum:
        return `Expected number to be greater or equal to ${error.schema.minimum}`;
      case ValueErrorType.NumberMultipleOf:
        return `Expected number to be a multiple of ${error.schema.multipleOf}`;
      case ValueErrorType.Number:
        return "Expected number";
      case ValueErrorType.Object:
        return "Expected object";
      case ValueErrorType.ObjectAdditionalProperties:
        return "Unexpected property";
      case ValueErrorType.ObjectMaxProperties:
        return `Expected object to have no more than ${error.schema.maxProperties} properties`;
      case ValueErrorType.ObjectMinProperties:
        return `Expected object to have at least ${error.schema.minProperties} properties`;
      case ValueErrorType.ObjectRequiredProperty:
        return "Expected required property";
      case ValueErrorType.Promise:
        return "Expected Promise";
      case ValueErrorType.RegExp:
        return "Expected string to match regular expression";
      case ValueErrorType.StringFormatUnknown:
        return `Unknown format '${error.schema.format}'`;
      case ValueErrorType.StringFormat:
        return `Expected string to match '${error.schema.format}' format`;
      case ValueErrorType.StringMaxLength:
        return `Expected string length less or equal to ${error.schema.maxLength}`;
      case ValueErrorType.StringMinLength:
        return `Expected string length greater or equal to ${error.schema.minLength}`;
      case ValueErrorType.StringPattern:
        return `Expected string to match '${error.schema.pattern}'`;
      case ValueErrorType.String:
        return "Expected string";
      case ValueErrorType.Symbol:
        return "Expected symbol";
      case ValueErrorType.TupleLength:
        return `Expected tuple to have ${error.schema.maxItems || 0} elements`;
      case ValueErrorType.Tuple:
        return "Expected tuple";
      case ValueErrorType.Uint8ArrayMaxByteLength:
        return `Expected byte length less or equal to ${error.schema.maxByteLength}`;
      case ValueErrorType.Uint8ArrayMinByteLength:
        return `Expected byte length greater or equal to ${error.schema.minByteLength}`;
      case ValueErrorType.Uint8Array:
        return "Expected Uint8Array";
      case ValueErrorType.Undefined:
        return "Expected undefined";
      case ValueErrorType.Union:
        return "Expected union value";
      case ValueErrorType.Void:
        return "Expected void";
      case ValueErrorType.Kind:
        return `Expected kind '${error.schema[Kind]}'`;
      default:
        return "Unknown error type";
    }
  }
  var errorFunction = DefaultErrorFunction;
  function GetErrorFunction() {
    return errorFunction;
  }

  // ../../node_modules/@sinclair/typebox/build/esm/value/deref/deref.mjs
  var TypeDereferenceError = class extends TypeBoxError {
    constructor(schema) {
      super(`Unable to dereference schema with $id '${schema.$ref}'`);
      this.schema = schema;
    }
  };
  function Resolve(schema, references) {
    const target = references.find((target2) => target2.$id === schema.$ref);
    if (target === void 0)
      throw new TypeDereferenceError(schema);
    return Deref(target, references);
  }
  function Pushref(schema, references) {
    if (!IsString2(schema.$id) || references.some((target) => target.$id === schema.$id))
      return references;
    references.push(schema);
    return references;
  }
  function Deref(schema, references) {
    return schema[Kind] === "This" || schema[Kind] === "Ref" ? Resolve(schema, references) : schema;
  }

  // ../../node_modules/@sinclair/typebox/build/esm/value/hash/hash.mjs
  var ValueHashError = class extends TypeBoxError {
    constructor(value) {
      super(`Unable to hash value`);
      this.value = value;
    }
  };
  var ByteMarker;
  (function(ByteMarker2) {
    ByteMarker2[ByteMarker2["Undefined"] = 0] = "Undefined";
    ByteMarker2[ByteMarker2["Null"] = 1] = "Null";
    ByteMarker2[ByteMarker2["Boolean"] = 2] = "Boolean";
    ByteMarker2[ByteMarker2["Number"] = 3] = "Number";
    ByteMarker2[ByteMarker2["String"] = 4] = "String";
    ByteMarker2[ByteMarker2["Object"] = 5] = "Object";
    ByteMarker2[ByteMarker2["Array"] = 6] = "Array";
    ByteMarker2[ByteMarker2["Date"] = 7] = "Date";
    ByteMarker2[ByteMarker2["Uint8Array"] = 8] = "Uint8Array";
    ByteMarker2[ByteMarker2["Symbol"] = 9] = "Symbol";
    ByteMarker2[ByteMarker2["BigInt"] = 10] = "BigInt";
  })(ByteMarker || (ByteMarker = {}));
  var Accumulator = BigInt("14695981039346656037");
  var [Prime, Size] = [BigInt("1099511628211"), BigInt(
    "18446744073709551616"
    /* 2 ^ 64 */
  )];
  var Bytes = Array.from({ length: 256 }).map((_, i) => BigInt(i));
  var F64 = new Float64Array(1);
  var F64In = new DataView(F64.buffer);
  var F64Out = new Uint8Array(F64.buffer);
  function* NumberToBytes(value) {
    const byteCount = value === 0 ? 1 : Math.ceil(Math.floor(Math.log2(value) + 1) / 8);
    for (let i = 0; i < byteCount; i++) {
      yield value >> 8 * (byteCount - 1 - i) & 255;
    }
  }
  function ArrayType2(value) {
    FNV1A64(ByteMarker.Array);
    for (const item of value) {
      Visit4(item);
    }
  }
  function BooleanType(value) {
    FNV1A64(ByteMarker.Boolean);
    FNV1A64(value ? 1 : 0);
  }
  function BigIntType(value) {
    FNV1A64(ByteMarker.BigInt);
    F64In.setBigInt64(0, value);
    for (const byte of F64Out) {
      FNV1A64(byte);
    }
  }
  function DateType2(value) {
    FNV1A64(ByteMarker.Date);
    Visit4(value.getTime());
  }
  function NullType(value) {
    FNV1A64(ByteMarker.Null);
  }
  function NumberType(value) {
    FNV1A64(ByteMarker.Number);
    F64In.setFloat64(0, value);
    for (const byte of F64Out) {
      FNV1A64(byte);
    }
  }
  function ObjectType2(value) {
    FNV1A64(ByteMarker.Object);
    for (const key of globalThis.Object.getOwnPropertyNames(value).sort()) {
      Visit4(key);
      Visit4(value[key]);
    }
  }
  function StringType(value) {
    FNV1A64(ByteMarker.String);
    for (let i = 0; i < value.length; i++) {
      for (const byte of NumberToBytes(value.charCodeAt(i))) {
        FNV1A64(byte);
      }
    }
  }
  function SymbolType(value) {
    FNV1A64(ByteMarker.Symbol);
    Visit4(value.description);
  }
  function Uint8ArrayType2(value) {
    FNV1A64(ByteMarker.Uint8Array);
    for (let i = 0; i < value.length; i++) {
      FNV1A64(value[i]);
    }
  }
  function UndefinedType(value) {
    return FNV1A64(ByteMarker.Undefined);
  }
  function Visit4(value) {
    if (IsArray2(value))
      return ArrayType2(value);
    if (IsBoolean2(value))
      return BooleanType(value);
    if (IsBigInt2(value))
      return BigIntType(value);
    if (IsDate2(value))
      return DateType2(value);
    if (IsNull2(value))
      return NullType(value);
    if (IsNumber2(value))
      return NumberType(value);
    if (IsObject2(value))
      return ObjectType2(value);
    if (IsString2(value))
      return StringType(value);
    if (IsSymbol2(value))
      return SymbolType(value);
    if (IsUint8Array2(value))
      return Uint8ArrayType2(value);
    if (IsUndefined2(value))
      return UndefinedType(value);
    throw new ValueHashError(value);
  }
  function FNV1A64(byte) {
    Accumulator = Accumulator ^ Bytes[byte];
    Accumulator = Accumulator * Prime % Size;
  }
  function Hash(value) {
    Accumulator = BigInt("14695981039346656037");
    Visit4(value);
    return Accumulator;
  }

  // ../../node_modules/@sinclair/typebox/build/esm/value/check/check.mjs
  var ValueCheckUnknownTypeError = class extends TypeBoxError {
    constructor(schema) {
      super(`Unknown type`);
      this.schema = schema;
    }
  };
  function IsAnyOrUnknown(schema) {
    return schema[Kind] === "Any" || schema[Kind] === "Unknown";
  }
  function IsDefined(value) {
    return value !== void 0;
  }
  function FromAny2(schema, references, value) {
    return true;
  }
  function FromArgument2(schema, references, value) {
    return true;
  }
  function FromArray7(schema, references, value) {
    if (!IsArray2(value))
      return false;
    if (IsDefined(schema.minItems) && !(value.length >= schema.minItems)) {
      return false;
    }
    if (IsDefined(schema.maxItems) && !(value.length <= schema.maxItems)) {
      return false;
    }
    for (const element of value) {
      if (!Visit5(schema.items, references, element))
        return false;
    }
    if (schema.uniqueItems === true && !function() {
      const set = /* @__PURE__ */ new Set();
      for (const element of value) {
        const hashed = Hash(element);
        if (set.has(hashed)) {
          return false;
        } else {
          set.add(hashed);
        }
      }
      return true;
    }()) {
      return false;
    }
    if (!(IsDefined(schema.contains) || IsNumber2(schema.minContains) || IsNumber2(schema.maxContains))) {
      return true;
    }
    const containsSchema = IsDefined(schema.contains) ? schema.contains : Never();
    const containsCount = value.reduce((acc, value2) => Visit5(containsSchema, references, value2) ? acc + 1 : acc, 0);
    if (containsCount === 0) {
      return false;
    }
    if (IsNumber2(schema.minContains) && containsCount < schema.minContains) {
      return false;
    }
    if (IsNumber2(schema.maxContains) && containsCount > schema.maxContains) {
      return false;
    }
    return true;
  }
  function FromAsyncIterator4(schema, references, value) {
    return IsAsyncIterator2(value);
  }
  function FromBigInt2(schema, references, value) {
    if (!IsBigInt2(value))
      return false;
    if (IsDefined(schema.exclusiveMaximum) && !(value < schema.exclusiveMaximum)) {
      return false;
    }
    if (IsDefined(schema.exclusiveMinimum) && !(value > schema.exclusiveMinimum)) {
      return false;
    }
    if (IsDefined(schema.maximum) && !(value <= schema.maximum)) {
      return false;
    }
    if (IsDefined(schema.minimum) && !(value >= schema.minimum)) {
      return false;
    }
    if (IsDefined(schema.multipleOf) && !(value % schema.multipleOf === BigInt(0))) {
      return false;
    }
    return true;
  }
  function FromBoolean2(schema, references, value) {
    return IsBoolean2(value);
  }
  function FromConstructor4(schema, references, value) {
    return Visit5(schema.returns, references, value.prototype);
  }
  function FromDate2(schema, references, value) {
    if (!IsDate2(value))
      return false;
    if (IsDefined(schema.exclusiveMaximumTimestamp) && !(value.getTime() < schema.exclusiveMaximumTimestamp)) {
      return false;
    }
    if (IsDefined(schema.exclusiveMinimumTimestamp) && !(value.getTime() > schema.exclusiveMinimumTimestamp)) {
      return false;
    }
    if (IsDefined(schema.maximumTimestamp) && !(value.getTime() <= schema.maximumTimestamp)) {
      return false;
    }
    if (IsDefined(schema.minimumTimestamp) && !(value.getTime() >= schema.minimumTimestamp)) {
      return false;
    }
    if (IsDefined(schema.multipleOfTimestamp) && !(value.getTime() % schema.multipleOfTimestamp === 0)) {
      return false;
    }
    return true;
  }
  function FromFunction4(schema, references, value) {
    return IsFunction2(value);
  }
  function FromImport(schema, references, value) {
    const definitions = globalThis.Object.values(schema.$defs);
    const target = schema.$defs[schema.$ref];
    return Visit5(target, [...references, ...definitions], value);
  }
  function FromInteger2(schema, references, value) {
    if (!IsInteger(value)) {
      return false;
    }
    if (IsDefined(schema.exclusiveMaximum) && !(value < schema.exclusiveMaximum)) {
      return false;
    }
    if (IsDefined(schema.exclusiveMinimum) && !(value > schema.exclusiveMinimum)) {
      return false;
    }
    if (IsDefined(schema.maximum) && !(value <= schema.maximum)) {
      return false;
    }
    if (IsDefined(schema.minimum) && !(value >= schema.minimum)) {
      return false;
    }
    if (IsDefined(schema.multipleOf) && !(value % schema.multipleOf === 0)) {
      return false;
    }
    return true;
  }
  function FromIntersect9(schema, references, value) {
    const check1 = schema.allOf.every((schema2) => Visit5(schema2, references, value));
    if (schema.unevaluatedProperties === false) {
      const keyPattern = new RegExp(KeyOfPattern(schema));
      const check2 = Object.getOwnPropertyNames(value).every((key) => keyPattern.test(key));
      return check1 && check2;
    } else if (IsSchema(schema.unevaluatedProperties)) {
      const keyCheck = new RegExp(KeyOfPattern(schema));
      const check2 = Object.getOwnPropertyNames(value).every((key) => keyCheck.test(key) || Visit5(schema.unevaluatedProperties, references, value[key]));
      return check1 && check2;
    } else {
      return check1;
    }
  }
  function FromIterator4(schema, references, value) {
    return IsIterator2(value);
  }
  function FromLiteral3(schema, references, value) {
    return value === schema.const;
  }
  function FromNever2(schema, references, value) {
    return false;
  }
  function FromNot2(schema, references, value) {
    return !Visit5(schema.not, references, value);
  }
  function FromNull2(schema, references, value) {
    return IsNull2(value);
  }
  function FromNumber2(schema, references, value) {
    if (!TypeSystemPolicy.IsNumberLike(value))
      return false;
    if (IsDefined(schema.exclusiveMaximum) && !(value < schema.exclusiveMaximum)) {
      return false;
    }
    if (IsDefined(schema.exclusiveMinimum) && !(value > schema.exclusiveMinimum)) {
      return false;
    }
    if (IsDefined(schema.minimum) && !(value >= schema.minimum)) {
      return false;
    }
    if (IsDefined(schema.maximum) && !(value <= schema.maximum)) {
      return false;
    }
    if (IsDefined(schema.multipleOf) && !(value % schema.multipleOf === 0)) {
      return false;
    }
    return true;
  }
  function FromObject8(schema, references, value) {
    if (!TypeSystemPolicy.IsObjectLike(value))
      return false;
    if (IsDefined(schema.minProperties) && !(Object.getOwnPropertyNames(value).length >= schema.minProperties)) {
      return false;
    }
    if (IsDefined(schema.maxProperties) && !(Object.getOwnPropertyNames(value).length <= schema.maxProperties)) {
      return false;
    }
    const knownKeys = Object.getOwnPropertyNames(schema.properties);
    for (const knownKey of knownKeys) {
      const property = schema.properties[knownKey];
      if (schema.required && schema.required.includes(knownKey)) {
        if (!Visit5(property, references, value[knownKey])) {
          return false;
        }
        if ((ExtendsUndefinedCheck(property) || IsAnyOrUnknown(property)) && !(knownKey in value)) {
          return false;
        }
      } else {
        if (TypeSystemPolicy.IsExactOptionalProperty(value, knownKey) && !Visit5(property, references, value[knownKey])) {
          return false;
        }
      }
    }
    if (schema.additionalProperties === false) {
      const valueKeys = Object.getOwnPropertyNames(value);
      if (schema.required && schema.required.length === knownKeys.length && valueKeys.length === knownKeys.length) {
        return true;
      } else {
        return valueKeys.every((valueKey) => knownKeys.includes(valueKey));
      }
    } else if (typeof schema.additionalProperties === "object") {
      const valueKeys = Object.getOwnPropertyNames(value);
      return valueKeys.every((key) => knownKeys.includes(key) || Visit5(schema.additionalProperties, references, value[key]));
    } else {
      return true;
    }
  }
  function FromPromise4(schema, references, value) {
    return IsPromise(value);
  }
  function FromRecord4(schema, references, value) {
    if (!TypeSystemPolicy.IsRecordLike(value)) {
      return false;
    }
    if (IsDefined(schema.minProperties) && !(Object.getOwnPropertyNames(value).length >= schema.minProperties)) {
      return false;
    }
    if (IsDefined(schema.maxProperties) && !(Object.getOwnPropertyNames(value).length <= schema.maxProperties)) {
      return false;
    }
    const [patternKey, patternSchema] = Object.entries(schema.patternProperties)[0];
    const regex = new RegExp(patternKey);
    const check1 = Object.entries(value).every(([key, value2]) => {
      return regex.test(key) ? Visit5(patternSchema, references, value2) : true;
    });
    const check2 = typeof schema.additionalProperties === "object" ? Object.entries(value).every(([key, value2]) => {
      return !regex.test(key) ? Visit5(schema.additionalProperties, references, value2) : true;
    }) : true;
    const check3 = schema.additionalProperties === false ? Object.getOwnPropertyNames(value).every((key) => {
      return regex.test(key);
    }) : true;
    return check1 && check2 && check3;
  }
  function FromRef5(schema, references, value) {
    return Visit5(Deref(schema, references), references, value);
  }
  function FromRegExp2(schema, references, value) {
    const regex = new RegExp(schema.source, schema.flags);
    if (IsDefined(schema.minLength)) {
      if (!(value.length >= schema.minLength))
        return false;
    }
    if (IsDefined(schema.maxLength)) {
      if (!(value.length <= schema.maxLength))
        return false;
    }
    return regex.test(value);
  }
  function FromString2(schema, references, value) {
    if (!IsString2(value)) {
      return false;
    }
    if (IsDefined(schema.minLength)) {
      if (!(value.length >= schema.minLength))
        return false;
    }
    if (IsDefined(schema.maxLength)) {
      if (!(value.length <= schema.maxLength))
        return false;
    }
    if (IsDefined(schema.pattern)) {
      const regex = new RegExp(schema.pattern);
      if (!regex.test(value))
        return false;
    }
    if (IsDefined(schema.format)) {
      if (!format_exports.Has(schema.format))
        return false;
      const func = format_exports.Get(schema.format);
      return func(value);
    }
    return true;
  }
  function FromSymbol2(schema, references, value) {
    return IsSymbol2(value);
  }
  function FromTemplateLiteral4(schema, references, value) {
    return IsString2(value) && new RegExp(schema.pattern).test(value);
  }
  function FromThis(schema, references, value) {
    return Visit5(Deref(schema, references), references, value);
  }
  function FromTuple6(schema, references, value) {
    if (!IsArray2(value)) {
      return false;
    }
    if (schema.items === void 0 && !(value.length === 0)) {
      return false;
    }
    if (!(value.length === schema.maxItems)) {
      return false;
    }
    if (!schema.items) {
      return true;
    }
    for (let i = 0; i < schema.items.length; i++) {
      if (!Visit5(schema.items[i], references, value[i]))
        return false;
    }
    return true;
  }
  function FromUndefined2(schema, references, value) {
    return IsUndefined2(value);
  }
  function FromUnion11(schema, references, value) {
    return schema.anyOf.some((inner) => Visit5(inner, references, value));
  }
  function FromUint8Array2(schema, references, value) {
    if (!IsUint8Array2(value)) {
      return false;
    }
    if (IsDefined(schema.maxByteLength) && !(value.length <= schema.maxByteLength)) {
      return false;
    }
    if (IsDefined(schema.minByteLength) && !(value.length >= schema.minByteLength)) {
      return false;
    }
    return true;
  }
  function FromUnknown2(schema, references, value) {
    return true;
  }
  function FromVoid2(schema, references, value) {
    return TypeSystemPolicy.IsVoidLike(value);
  }
  function FromKind(schema, references, value) {
    if (!type_exports2.Has(schema[Kind]))
      return false;
    const func = type_exports2.Get(schema[Kind]);
    return func(schema, value);
  }
  function Visit5(schema, references, value) {
    const references_ = IsDefined(schema.$id) ? Pushref(schema, references) : references;
    const schema_ = schema;
    switch (schema_[Kind]) {
      case "Any":
        return FromAny2(schema_, references_, value);
      case "Argument":
        return FromArgument2(schema_, references_, value);
      case "Array":
        return FromArray7(schema_, references_, value);
      case "AsyncIterator":
        return FromAsyncIterator4(schema_, references_, value);
      case "BigInt":
        return FromBigInt2(schema_, references_, value);
      case "Boolean":
        return FromBoolean2(schema_, references_, value);
      case "Constructor":
        return FromConstructor4(schema_, references_, value);
      case "Date":
        return FromDate2(schema_, references_, value);
      case "Function":
        return FromFunction4(schema_, references_, value);
      case "Import":
        return FromImport(schema_, references_, value);
      case "Integer":
        return FromInteger2(schema_, references_, value);
      case "Intersect":
        return FromIntersect9(schema_, references_, value);
      case "Iterator":
        return FromIterator4(schema_, references_, value);
      case "Literal":
        return FromLiteral3(schema_, references_, value);
      case "Never":
        return FromNever2(schema_, references_, value);
      case "Not":
        return FromNot2(schema_, references_, value);
      case "Null":
        return FromNull2(schema_, references_, value);
      case "Number":
        return FromNumber2(schema_, references_, value);
      case "Object":
        return FromObject8(schema_, references_, value);
      case "Promise":
        return FromPromise4(schema_, references_, value);
      case "Record":
        return FromRecord4(schema_, references_, value);
      case "Ref":
        return FromRef5(schema_, references_, value);
      case "RegExp":
        return FromRegExp2(schema_, references_, value);
      case "String":
        return FromString2(schema_, references_, value);
      case "Symbol":
        return FromSymbol2(schema_, references_, value);
      case "TemplateLiteral":
        return FromTemplateLiteral4(schema_, references_, value);
      case "This":
        return FromThis(schema_, references_, value);
      case "Tuple":
        return FromTuple6(schema_, references_, value);
      case "Undefined":
        return FromUndefined2(schema_, references_, value);
      case "Union":
        return FromUnion11(schema_, references_, value);
      case "Uint8Array":
        return FromUint8Array2(schema_, references_, value);
      case "Unknown":
        return FromUnknown2(schema_, references_, value);
      case "Void":
        return FromVoid2(schema_, references_, value);
      default:
        if (!type_exports2.Has(schema_[Kind]))
          throw new ValueCheckUnknownTypeError(schema_);
        return FromKind(schema_, references_, value);
    }
  }
  function Check(...args) {
    return args.length === 3 ? Visit5(args[0], args[1], args[2]) : Visit5(args[0], [], args[1]);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/errors/errors.mjs
  var ValueErrorType;
  (function(ValueErrorType2) {
    ValueErrorType2[ValueErrorType2["ArrayContains"] = 0] = "ArrayContains";
    ValueErrorType2[ValueErrorType2["ArrayMaxContains"] = 1] = "ArrayMaxContains";
    ValueErrorType2[ValueErrorType2["ArrayMaxItems"] = 2] = "ArrayMaxItems";
    ValueErrorType2[ValueErrorType2["ArrayMinContains"] = 3] = "ArrayMinContains";
    ValueErrorType2[ValueErrorType2["ArrayMinItems"] = 4] = "ArrayMinItems";
    ValueErrorType2[ValueErrorType2["ArrayUniqueItems"] = 5] = "ArrayUniqueItems";
    ValueErrorType2[ValueErrorType2["Array"] = 6] = "Array";
    ValueErrorType2[ValueErrorType2["AsyncIterator"] = 7] = "AsyncIterator";
    ValueErrorType2[ValueErrorType2["BigIntExclusiveMaximum"] = 8] = "BigIntExclusiveMaximum";
    ValueErrorType2[ValueErrorType2["BigIntExclusiveMinimum"] = 9] = "BigIntExclusiveMinimum";
    ValueErrorType2[ValueErrorType2["BigIntMaximum"] = 10] = "BigIntMaximum";
    ValueErrorType2[ValueErrorType2["BigIntMinimum"] = 11] = "BigIntMinimum";
    ValueErrorType2[ValueErrorType2["BigIntMultipleOf"] = 12] = "BigIntMultipleOf";
    ValueErrorType2[ValueErrorType2["BigInt"] = 13] = "BigInt";
    ValueErrorType2[ValueErrorType2["Boolean"] = 14] = "Boolean";
    ValueErrorType2[ValueErrorType2["DateExclusiveMaximumTimestamp"] = 15] = "DateExclusiveMaximumTimestamp";
    ValueErrorType2[ValueErrorType2["DateExclusiveMinimumTimestamp"] = 16] = "DateExclusiveMinimumTimestamp";
    ValueErrorType2[ValueErrorType2["DateMaximumTimestamp"] = 17] = "DateMaximumTimestamp";
    ValueErrorType2[ValueErrorType2["DateMinimumTimestamp"] = 18] = "DateMinimumTimestamp";
    ValueErrorType2[ValueErrorType2["DateMultipleOfTimestamp"] = 19] = "DateMultipleOfTimestamp";
    ValueErrorType2[ValueErrorType2["Date"] = 20] = "Date";
    ValueErrorType2[ValueErrorType2["Function"] = 21] = "Function";
    ValueErrorType2[ValueErrorType2["IntegerExclusiveMaximum"] = 22] = "IntegerExclusiveMaximum";
    ValueErrorType2[ValueErrorType2["IntegerExclusiveMinimum"] = 23] = "IntegerExclusiveMinimum";
    ValueErrorType2[ValueErrorType2["IntegerMaximum"] = 24] = "IntegerMaximum";
    ValueErrorType2[ValueErrorType2["IntegerMinimum"] = 25] = "IntegerMinimum";
    ValueErrorType2[ValueErrorType2["IntegerMultipleOf"] = 26] = "IntegerMultipleOf";
    ValueErrorType2[ValueErrorType2["Integer"] = 27] = "Integer";
    ValueErrorType2[ValueErrorType2["IntersectUnevaluatedProperties"] = 28] = "IntersectUnevaluatedProperties";
    ValueErrorType2[ValueErrorType2["Intersect"] = 29] = "Intersect";
    ValueErrorType2[ValueErrorType2["Iterator"] = 30] = "Iterator";
    ValueErrorType2[ValueErrorType2["Kind"] = 31] = "Kind";
    ValueErrorType2[ValueErrorType2["Literal"] = 32] = "Literal";
    ValueErrorType2[ValueErrorType2["Never"] = 33] = "Never";
    ValueErrorType2[ValueErrorType2["Not"] = 34] = "Not";
    ValueErrorType2[ValueErrorType2["Null"] = 35] = "Null";
    ValueErrorType2[ValueErrorType2["NumberExclusiveMaximum"] = 36] = "NumberExclusiveMaximum";
    ValueErrorType2[ValueErrorType2["NumberExclusiveMinimum"] = 37] = "NumberExclusiveMinimum";
    ValueErrorType2[ValueErrorType2["NumberMaximum"] = 38] = "NumberMaximum";
    ValueErrorType2[ValueErrorType2["NumberMinimum"] = 39] = "NumberMinimum";
    ValueErrorType2[ValueErrorType2["NumberMultipleOf"] = 40] = "NumberMultipleOf";
    ValueErrorType2[ValueErrorType2["Number"] = 41] = "Number";
    ValueErrorType2[ValueErrorType2["ObjectAdditionalProperties"] = 42] = "ObjectAdditionalProperties";
    ValueErrorType2[ValueErrorType2["ObjectMaxProperties"] = 43] = "ObjectMaxProperties";
    ValueErrorType2[ValueErrorType2["ObjectMinProperties"] = 44] = "ObjectMinProperties";
    ValueErrorType2[ValueErrorType2["ObjectRequiredProperty"] = 45] = "ObjectRequiredProperty";
    ValueErrorType2[ValueErrorType2["Object"] = 46] = "Object";
    ValueErrorType2[ValueErrorType2["Promise"] = 47] = "Promise";
    ValueErrorType2[ValueErrorType2["RegExp"] = 48] = "RegExp";
    ValueErrorType2[ValueErrorType2["StringFormatUnknown"] = 49] = "StringFormatUnknown";
    ValueErrorType2[ValueErrorType2["StringFormat"] = 50] = "StringFormat";
    ValueErrorType2[ValueErrorType2["StringMaxLength"] = 51] = "StringMaxLength";
    ValueErrorType2[ValueErrorType2["StringMinLength"] = 52] = "StringMinLength";
    ValueErrorType2[ValueErrorType2["StringPattern"] = 53] = "StringPattern";
    ValueErrorType2[ValueErrorType2["String"] = 54] = "String";
    ValueErrorType2[ValueErrorType2["Symbol"] = 55] = "Symbol";
    ValueErrorType2[ValueErrorType2["TupleLength"] = 56] = "TupleLength";
    ValueErrorType2[ValueErrorType2["Tuple"] = 57] = "Tuple";
    ValueErrorType2[ValueErrorType2["Uint8ArrayMaxByteLength"] = 58] = "Uint8ArrayMaxByteLength";
    ValueErrorType2[ValueErrorType2["Uint8ArrayMinByteLength"] = 59] = "Uint8ArrayMinByteLength";
    ValueErrorType2[ValueErrorType2["Uint8Array"] = 60] = "Uint8Array";
    ValueErrorType2[ValueErrorType2["Undefined"] = 61] = "Undefined";
    ValueErrorType2[ValueErrorType2["Union"] = 62] = "Union";
    ValueErrorType2[ValueErrorType2["Void"] = 63] = "Void";
  })(ValueErrorType || (ValueErrorType = {}));
  var ValueErrorsUnknownTypeError = class extends TypeBoxError {
    constructor(schema) {
      super("Unknown type");
      this.schema = schema;
    }
  };
  function EscapeKey(key) {
    return key.replace(/~/g, "~0").replace(/\//g, "~1");
  }
  function IsDefined2(value) {
    return value !== void 0;
  }
  var ValueErrorIterator = class {
    constructor(iterator) {
      this.iterator = iterator;
    }
    [Symbol.iterator]() {
      return this.iterator;
    }
    /** Returns the first value error or undefined if no errors */
    First() {
      const next = this.iterator.next();
      return next.done ? void 0 : next.value;
    }
  };
  function Create(errorType, schema, path, value, errors = []) {
    return {
      type: errorType,
      schema,
      path,
      value,
      message: GetErrorFunction()({ errorType, path, schema, value, errors }),
      errors
    };
  }
  function* FromAny3(schema, references, path, value) {
  }
  function* FromArgument3(schema, references, path, value) {
  }
  function* FromArray8(schema, references, path, value) {
    if (!IsArray2(value)) {
      return yield Create(ValueErrorType.Array, schema, path, value);
    }
    if (IsDefined2(schema.minItems) && !(value.length >= schema.minItems)) {
      yield Create(ValueErrorType.ArrayMinItems, schema, path, value);
    }
    if (IsDefined2(schema.maxItems) && !(value.length <= schema.maxItems)) {
      yield Create(ValueErrorType.ArrayMaxItems, schema, path, value);
    }
    for (let i = 0; i < value.length; i++) {
      yield* Visit6(schema.items, references, `${path}/${i}`, value[i]);
    }
    if (schema.uniqueItems === true && !function() {
      const set = /* @__PURE__ */ new Set();
      for (const element of value) {
        const hashed = Hash(element);
        if (set.has(hashed)) {
          return false;
        } else {
          set.add(hashed);
        }
      }
      return true;
    }()) {
      yield Create(ValueErrorType.ArrayUniqueItems, schema, path, value);
    }
    if (!(IsDefined2(schema.contains) || IsDefined2(schema.minContains) || IsDefined2(schema.maxContains))) {
      return;
    }
    const containsSchema = IsDefined2(schema.contains) ? schema.contains : Never();
    const containsCount = value.reduce((acc, value2, index) => Visit6(containsSchema, references, `${path}${index}`, value2).next().done === true ? acc + 1 : acc, 0);
    if (containsCount === 0) {
      yield Create(ValueErrorType.ArrayContains, schema, path, value);
    }
    if (IsNumber2(schema.minContains) && containsCount < schema.minContains) {
      yield Create(ValueErrorType.ArrayMinContains, schema, path, value);
    }
    if (IsNumber2(schema.maxContains) && containsCount > schema.maxContains) {
      yield Create(ValueErrorType.ArrayMaxContains, schema, path, value);
    }
  }
  function* FromAsyncIterator5(schema, references, path, value) {
    if (!IsAsyncIterator2(value))
      yield Create(ValueErrorType.AsyncIterator, schema, path, value);
  }
  function* FromBigInt3(schema, references, path, value) {
    if (!IsBigInt2(value))
      return yield Create(ValueErrorType.BigInt, schema, path, value);
    if (IsDefined2(schema.exclusiveMaximum) && !(value < schema.exclusiveMaximum)) {
      yield Create(ValueErrorType.BigIntExclusiveMaximum, schema, path, value);
    }
    if (IsDefined2(schema.exclusiveMinimum) && !(value > schema.exclusiveMinimum)) {
      yield Create(ValueErrorType.BigIntExclusiveMinimum, schema, path, value);
    }
    if (IsDefined2(schema.maximum) && !(value <= schema.maximum)) {
      yield Create(ValueErrorType.BigIntMaximum, schema, path, value);
    }
    if (IsDefined2(schema.minimum) && !(value >= schema.minimum)) {
      yield Create(ValueErrorType.BigIntMinimum, schema, path, value);
    }
    if (IsDefined2(schema.multipleOf) && !(value % schema.multipleOf === BigInt(0))) {
      yield Create(ValueErrorType.BigIntMultipleOf, schema, path, value);
    }
  }
  function* FromBoolean3(schema, references, path, value) {
    if (!IsBoolean2(value))
      yield Create(ValueErrorType.Boolean, schema, path, value);
  }
  function* FromConstructor5(schema, references, path, value) {
    yield* Visit6(schema.returns, references, path, value.prototype);
  }
  function* FromDate3(schema, references, path, value) {
    if (!IsDate2(value))
      return yield Create(ValueErrorType.Date, schema, path, value);
    if (IsDefined2(schema.exclusiveMaximumTimestamp) && !(value.getTime() < schema.exclusiveMaximumTimestamp)) {
      yield Create(ValueErrorType.DateExclusiveMaximumTimestamp, schema, path, value);
    }
    if (IsDefined2(schema.exclusiveMinimumTimestamp) && !(value.getTime() > schema.exclusiveMinimumTimestamp)) {
      yield Create(ValueErrorType.DateExclusiveMinimumTimestamp, schema, path, value);
    }
    if (IsDefined2(schema.maximumTimestamp) && !(value.getTime() <= schema.maximumTimestamp)) {
      yield Create(ValueErrorType.DateMaximumTimestamp, schema, path, value);
    }
    if (IsDefined2(schema.minimumTimestamp) && !(value.getTime() >= schema.minimumTimestamp)) {
      yield Create(ValueErrorType.DateMinimumTimestamp, schema, path, value);
    }
    if (IsDefined2(schema.multipleOfTimestamp) && !(value.getTime() % schema.multipleOfTimestamp === 0)) {
      yield Create(ValueErrorType.DateMultipleOfTimestamp, schema, path, value);
    }
  }
  function* FromFunction5(schema, references, path, value) {
    if (!IsFunction2(value))
      yield Create(ValueErrorType.Function, schema, path, value);
  }
  function* FromImport2(schema, references, path, value) {
    const definitions = globalThis.Object.values(schema.$defs);
    const target = schema.$defs[schema.$ref];
    yield* Visit6(target, [...references, ...definitions], path, value);
  }
  function* FromInteger3(schema, references, path, value) {
    if (!IsInteger(value))
      return yield Create(ValueErrorType.Integer, schema, path, value);
    if (IsDefined2(schema.exclusiveMaximum) && !(value < schema.exclusiveMaximum)) {
      yield Create(ValueErrorType.IntegerExclusiveMaximum, schema, path, value);
    }
    if (IsDefined2(schema.exclusiveMinimum) && !(value > schema.exclusiveMinimum)) {
      yield Create(ValueErrorType.IntegerExclusiveMinimum, schema, path, value);
    }
    if (IsDefined2(schema.maximum) && !(value <= schema.maximum)) {
      yield Create(ValueErrorType.IntegerMaximum, schema, path, value);
    }
    if (IsDefined2(schema.minimum) && !(value >= schema.minimum)) {
      yield Create(ValueErrorType.IntegerMinimum, schema, path, value);
    }
    if (IsDefined2(schema.multipleOf) && !(value % schema.multipleOf === 0)) {
      yield Create(ValueErrorType.IntegerMultipleOf, schema, path, value);
    }
  }
  function* FromIntersect10(schema, references, path, value) {
    let hasError = false;
    for (const inner of schema.allOf) {
      for (const error of Visit6(inner, references, path, value)) {
        hasError = true;
        yield error;
      }
    }
    if (hasError) {
      return yield Create(ValueErrorType.Intersect, schema, path, value);
    }
    if (schema.unevaluatedProperties === false) {
      const keyCheck = new RegExp(KeyOfPattern(schema));
      for (const valueKey of Object.getOwnPropertyNames(value)) {
        if (!keyCheck.test(valueKey)) {
          yield Create(ValueErrorType.IntersectUnevaluatedProperties, schema, `${path}/${valueKey}`, value);
        }
      }
    }
    if (typeof schema.unevaluatedProperties === "object") {
      const keyCheck = new RegExp(KeyOfPattern(schema));
      for (const valueKey of Object.getOwnPropertyNames(value)) {
        if (!keyCheck.test(valueKey)) {
          const next = Visit6(schema.unevaluatedProperties, references, `${path}/${valueKey}`, value[valueKey]).next();
          if (!next.done)
            yield next.value;
        }
      }
    }
  }
  function* FromIterator5(schema, references, path, value) {
    if (!IsIterator2(value))
      yield Create(ValueErrorType.Iterator, schema, path, value);
  }
  function* FromLiteral4(schema, references, path, value) {
    if (!(value === schema.const))
      yield Create(ValueErrorType.Literal, schema, path, value);
  }
  function* FromNever3(schema, references, path, value) {
    yield Create(ValueErrorType.Never, schema, path, value);
  }
  function* FromNot3(schema, references, path, value) {
    if (Visit6(schema.not, references, path, value).next().done === true)
      yield Create(ValueErrorType.Not, schema, path, value);
  }
  function* FromNull3(schema, references, path, value) {
    if (!IsNull2(value))
      yield Create(ValueErrorType.Null, schema, path, value);
  }
  function* FromNumber3(schema, references, path, value) {
    if (!TypeSystemPolicy.IsNumberLike(value))
      return yield Create(ValueErrorType.Number, schema, path, value);
    if (IsDefined2(schema.exclusiveMaximum) && !(value < schema.exclusiveMaximum)) {
      yield Create(ValueErrorType.NumberExclusiveMaximum, schema, path, value);
    }
    if (IsDefined2(schema.exclusiveMinimum) && !(value > schema.exclusiveMinimum)) {
      yield Create(ValueErrorType.NumberExclusiveMinimum, schema, path, value);
    }
    if (IsDefined2(schema.maximum) && !(value <= schema.maximum)) {
      yield Create(ValueErrorType.NumberMaximum, schema, path, value);
    }
    if (IsDefined2(schema.minimum) && !(value >= schema.minimum)) {
      yield Create(ValueErrorType.NumberMinimum, schema, path, value);
    }
    if (IsDefined2(schema.multipleOf) && !(value % schema.multipleOf === 0)) {
      yield Create(ValueErrorType.NumberMultipleOf, schema, path, value);
    }
  }
  function* FromObject9(schema, references, path, value) {
    if (!TypeSystemPolicy.IsObjectLike(value))
      return yield Create(ValueErrorType.Object, schema, path, value);
    if (IsDefined2(schema.minProperties) && !(Object.getOwnPropertyNames(value).length >= schema.minProperties)) {
      yield Create(ValueErrorType.ObjectMinProperties, schema, path, value);
    }
    if (IsDefined2(schema.maxProperties) && !(Object.getOwnPropertyNames(value).length <= schema.maxProperties)) {
      yield Create(ValueErrorType.ObjectMaxProperties, schema, path, value);
    }
    const requiredKeys = Array.isArray(schema.required) ? schema.required : [];
    const knownKeys = Object.getOwnPropertyNames(schema.properties);
    const unknownKeys = Object.getOwnPropertyNames(value);
    for (const requiredKey of requiredKeys) {
      if (unknownKeys.includes(requiredKey))
        continue;
      yield Create(ValueErrorType.ObjectRequiredProperty, schema.properties[requiredKey], `${path}/${EscapeKey(requiredKey)}`, void 0);
    }
    if (schema.additionalProperties === false) {
      for (const valueKey of unknownKeys) {
        if (!knownKeys.includes(valueKey)) {
          yield Create(ValueErrorType.ObjectAdditionalProperties, schema, `${path}/${EscapeKey(valueKey)}`, value[valueKey]);
        }
      }
    }
    if (typeof schema.additionalProperties === "object") {
      for (const valueKey of unknownKeys) {
        if (knownKeys.includes(valueKey))
          continue;
        yield* Visit6(schema.additionalProperties, references, `${path}/${EscapeKey(valueKey)}`, value[valueKey]);
      }
    }
    for (const knownKey of knownKeys) {
      const property = schema.properties[knownKey];
      if (schema.required && schema.required.includes(knownKey)) {
        yield* Visit6(property, references, `${path}/${EscapeKey(knownKey)}`, value[knownKey]);
        if (ExtendsUndefinedCheck(schema) && !(knownKey in value)) {
          yield Create(ValueErrorType.ObjectRequiredProperty, property, `${path}/${EscapeKey(knownKey)}`, void 0);
        }
      } else {
        if (TypeSystemPolicy.IsExactOptionalProperty(value, knownKey)) {
          yield* Visit6(property, references, `${path}/${EscapeKey(knownKey)}`, value[knownKey]);
        }
      }
    }
  }
  function* FromPromise5(schema, references, path, value) {
    if (!IsPromise(value))
      yield Create(ValueErrorType.Promise, schema, path, value);
  }
  function* FromRecord5(schema, references, path, value) {
    if (!TypeSystemPolicy.IsRecordLike(value))
      return yield Create(ValueErrorType.Object, schema, path, value);
    if (IsDefined2(schema.minProperties) && !(Object.getOwnPropertyNames(value).length >= schema.minProperties)) {
      yield Create(ValueErrorType.ObjectMinProperties, schema, path, value);
    }
    if (IsDefined2(schema.maxProperties) && !(Object.getOwnPropertyNames(value).length <= schema.maxProperties)) {
      yield Create(ValueErrorType.ObjectMaxProperties, schema, path, value);
    }
    const [patternKey, patternSchema] = Object.entries(schema.patternProperties)[0];
    const regex = new RegExp(patternKey);
    for (const [propertyKey, propertyValue] of Object.entries(value)) {
      if (regex.test(propertyKey))
        yield* Visit6(patternSchema, references, `${path}/${EscapeKey(propertyKey)}`, propertyValue);
    }
    if (typeof schema.additionalProperties === "object") {
      for (const [propertyKey, propertyValue] of Object.entries(value)) {
        if (!regex.test(propertyKey))
          yield* Visit6(schema.additionalProperties, references, `${path}/${EscapeKey(propertyKey)}`, propertyValue);
      }
    }
    if (schema.additionalProperties === false) {
      for (const [propertyKey, propertyValue] of Object.entries(value)) {
        if (regex.test(propertyKey))
          continue;
        return yield Create(ValueErrorType.ObjectAdditionalProperties, schema, `${path}/${EscapeKey(propertyKey)}`, propertyValue);
      }
    }
  }
  function* FromRef6(schema, references, path, value) {
    yield* Visit6(Deref(schema, references), references, path, value);
  }
  function* FromRegExp3(schema, references, path, value) {
    if (!IsString2(value))
      return yield Create(ValueErrorType.String, schema, path, value);
    if (IsDefined2(schema.minLength) && !(value.length >= schema.minLength)) {
      yield Create(ValueErrorType.StringMinLength, schema, path, value);
    }
    if (IsDefined2(schema.maxLength) && !(value.length <= schema.maxLength)) {
      yield Create(ValueErrorType.StringMaxLength, schema, path, value);
    }
    const regex = new RegExp(schema.source, schema.flags);
    if (!regex.test(value)) {
      return yield Create(ValueErrorType.RegExp, schema, path, value);
    }
  }
  function* FromString3(schema, references, path, value) {
    if (!IsString2(value))
      return yield Create(ValueErrorType.String, schema, path, value);
    if (IsDefined2(schema.minLength) && !(value.length >= schema.minLength)) {
      yield Create(ValueErrorType.StringMinLength, schema, path, value);
    }
    if (IsDefined2(schema.maxLength) && !(value.length <= schema.maxLength)) {
      yield Create(ValueErrorType.StringMaxLength, schema, path, value);
    }
    if (IsString2(schema.pattern)) {
      const regex = new RegExp(schema.pattern);
      if (!regex.test(value)) {
        yield Create(ValueErrorType.StringPattern, schema, path, value);
      }
    }
    if (IsString2(schema.format)) {
      if (!format_exports.Has(schema.format)) {
        yield Create(ValueErrorType.StringFormatUnknown, schema, path, value);
      } else {
        const format = format_exports.Get(schema.format);
        if (!format(value)) {
          yield Create(ValueErrorType.StringFormat, schema, path, value);
        }
      }
    }
  }
  function* FromSymbol3(schema, references, path, value) {
    if (!IsSymbol2(value))
      yield Create(ValueErrorType.Symbol, schema, path, value);
  }
  function* FromTemplateLiteral5(schema, references, path, value) {
    if (!IsString2(value))
      return yield Create(ValueErrorType.String, schema, path, value);
    const regex = new RegExp(schema.pattern);
    if (!regex.test(value)) {
      yield Create(ValueErrorType.StringPattern, schema, path, value);
    }
  }
  function* FromThis2(schema, references, path, value) {
    yield* Visit6(Deref(schema, references), references, path, value);
  }
  function* FromTuple7(schema, references, path, value) {
    if (!IsArray2(value))
      return yield Create(ValueErrorType.Tuple, schema, path, value);
    if (schema.items === void 0 && !(value.length === 0)) {
      return yield Create(ValueErrorType.TupleLength, schema, path, value);
    }
    if (!(value.length === schema.maxItems)) {
      return yield Create(ValueErrorType.TupleLength, schema, path, value);
    }
    if (!schema.items) {
      return;
    }
    for (let i = 0; i < schema.items.length; i++) {
      yield* Visit6(schema.items[i], references, `${path}/${i}`, value[i]);
    }
  }
  function* FromUndefined3(schema, references, path, value) {
    if (!IsUndefined2(value))
      yield Create(ValueErrorType.Undefined, schema, path, value);
  }
  function* FromUnion12(schema, references, path, value) {
    if (Check(schema, references, value))
      return;
    const errors = schema.anyOf.map((variant) => new ValueErrorIterator(Visit6(variant, references, path, value)));
    yield Create(ValueErrorType.Union, schema, path, value, errors);
  }
  function* FromUint8Array3(schema, references, path, value) {
    if (!IsUint8Array2(value))
      return yield Create(ValueErrorType.Uint8Array, schema, path, value);
    if (IsDefined2(schema.maxByteLength) && !(value.length <= schema.maxByteLength)) {
      yield Create(ValueErrorType.Uint8ArrayMaxByteLength, schema, path, value);
    }
    if (IsDefined2(schema.minByteLength) && !(value.length >= schema.minByteLength)) {
      yield Create(ValueErrorType.Uint8ArrayMinByteLength, schema, path, value);
    }
  }
  function* FromUnknown3(schema, references, path, value) {
  }
  function* FromVoid3(schema, references, path, value) {
    if (!TypeSystemPolicy.IsVoidLike(value))
      yield Create(ValueErrorType.Void, schema, path, value);
  }
  function* FromKind2(schema, references, path, value) {
    const check = type_exports2.Get(schema[Kind]);
    if (!check(schema, value))
      yield Create(ValueErrorType.Kind, schema, path, value);
  }
  function* Visit6(schema, references, path, value) {
    const references_ = IsDefined2(schema.$id) ? [...references, schema] : references;
    const schema_ = schema;
    switch (schema_[Kind]) {
      case "Any":
        return yield* FromAny3(schema_, references_, path, value);
      case "Argument":
        return yield* FromArgument3(schema_, references_, path, value);
      case "Array":
        return yield* FromArray8(schema_, references_, path, value);
      case "AsyncIterator":
        return yield* FromAsyncIterator5(schema_, references_, path, value);
      case "BigInt":
        return yield* FromBigInt3(schema_, references_, path, value);
      case "Boolean":
        return yield* FromBoolean3(schema_, references_, path, value);
      case "Constructor":
        return yield* FromConstructor5(schema_, references_, path, value);
      case "Date":
        return yield* FromDate3(schema_, references_, path, value);
      case "Function":
        return yield* FromFunction5(schema_, references_, path, value);
      case "Import":
        return yield* FromImport2(schema_, references_, path, value);
      case "Integer":
        return yield* FromInteger3(schema_, references_, path, value);
      case "Intersect":
        return yield* FromIntersect10(schema_, references_, path, value);
      case "Iterator":
        return yield* FromIterator5(schema_, references_, path, value);
      case "Literal":
        return yield* FromLiteral4(schema_, references_, path, value);
      case "Never":
        return yield* FromNever3(schema_, references_, path, value);
      case "Not":
        return yield* FromNot3(schema_, references_, path, value);
      case "Null":
        return yield* FromNull3(schema_, references_, path, value);
      case "Number":
        return yield* FromNumber3(schema_, references_, path, value);
      case "Object":
        return yield* FromObject9(schema_, references_, path, value);
      case "Promise":
        return yield* FromPromise5(schema_, references_, path, value);
      case "Record":
        return yield* FromRecord5(schema_, references_, path, value);
      case "Ref":
        return yield* FromRef6(schema_, references_, path, value);
      case "RegExp":
        return yield* FromRegExp3(schema_, references_, path, value);
      case "String":
        return yield* FromString3(schema_, references_, path, value);
      case "Symbol":
        return yield* FromSymbol3(schema_, references_, path, value);
      case "TemplateLiteral":
        return yield* FromTemplateLiteral5(schema_, references_, path, value);
      case "This":
        return yield* FromThis2(schema_, references_, path, value);
      case "Tuple":
        return yield* FromTuple7(schema_, references_, path, value);
      case "Undefined":
        return yield* FromUndefined3(schema_, references_, path, value);
      case "Union":
        return yield* FromUnion12(schema_, references_, path, value);
      case "Uint8Array":
        return yield* FromUint8Array3(schema_, references_, path, value);
      case "Unknown":
        return yield* FromUnknown3(schema_, references_, path, value);
      case "Void":
        return yield* FromVoid3(schema_, references_, path, value);
      default:
        if (!type_exports2.Has(schema_[Kind]))
          throw new ValueErrorsUnknownTypeError(schema);
        return yield* FromKind2(schema_, references_, path, value);
    }
  }
  function Errors(...args) {
    const iterator = args.length === 3 ? Visit6(args[0], args[1], "", args[2]) : Visit6(args[0], [], "", args[1]);
    return new ValueErrorIterator(iterator);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/value/assert/assert.mjs
  var __classPrivateFieldSet5 = function(receiver, state, value, kind2, f) {
    if (kind2 === "m") throw new TypeError("Private method is not writable");
    if (kind2 === "a" && !f) throw new TypeError("Private accessor was defined without a setter");
    if (typeof state === "function" ? receiver !== state || !f : !state.has(receiver)) throw new TypeError("Cannot write private member to an object whose class did not declare it");
    return kind2 === "a" ? f.call(receiver, value) : f ? f.value = value : state.set(receiver, value), value;
  };
  var __classPrivateFieldGet5 = function(receiver, state, kind2, f) {
    if (kind2 === "a" && !f) throw new TypeError("Private accessor was defined without a getter");
    if (typeof state === "function" ? receiver !== state || !f : !state.has(receiver)) throw new TypeError("Cannot read private member from an object whose class did not declare it");
    return kind2 === "m" ? f : kind2 === "a" ? f.call(receiver) : f ? f.value : state.get(receiver);
  };
  var _AssertError_instances;
  var _AssertError_iterator;
  var _AssertError_Iterator;
  var AssertError = class extends TypeBoxError {
    constructor(iterator) {
      const error = iterator.First();
      super(error === void 0 ? "Invalid Value" : error.message);
      _AssertError_instances.add(this);
      _AssertError_iterator.set(this, void 0);
      __classPrivateFieldSet5(this, _AssertError_iterator, iterator, "f");
      this.error = error;
    }
    /** Returns an iterator for each error in this value. */
    Errors() {
      return new ValueErrorIterator(__classPrivateFieldGet5(this, _AssertError_instances, "m", _AssertError_Iterator).call(this));
    }
  };
  _AssertError_iterator = /* @__PURE__ */ new WeakMap(), _AssertError_instances = /* @__PURE__ */ new WeakSet(), _AssertError_Iterator = function* _AssertError_Iterator2() {
    if (this.error)
      yield this.error;
    yield* __classPrivateFieldGet5(this, _AssertError_iterator, "f");
  };
  function AssertValue(schema, references, value) {
    if (Check(schema, references, value))
      return;
    throw new AssertError(Errors(schema, references, value));
  }
  function Assert(...args) {
    return args.length === 3 ? AssertValue(args[0], args[1], args[2]) : AssertValue(args[0], [], args[1]);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/value/clone/clone.mjs
  function FromObject10(value) {
    const Acc = {};
    for (const key of Object.getOwnPropertyNames(value)) {
      Acc[key] = Clone2(value[key]);
    }
    for (const key of Object.getOwnPropertySymbols(value)) {
      Acc[key] = Clone2(value[key]);
    }
    return Acc;
  }
  function FromArray9(value) {
    return value.map((element) => Clone2(element));
  }
  function FromTypedArray(value) {
    return value.slice();
  }
  function FromMap(value) {
    return new Map(Clone2([...value.entries()]));
  }
  function FromSet(value) {
    return new Set(Clone2([...value.entries()]));
  }
  function FromDate4(value) {
    return new Date(value.toISOString());
  }
  function FromValue2(value) {
    return value;
  }
  function Clone2(value) {
    if (IsArray2(value))
      return FromArray9(value);
    if (IsDate2(value))
      return FromDate4(value);
    if (IsTypedArray(value))
      return FromTypedArray(value);
    if (IsMap(value))
      return FromMap(value);
    if (IsSet(value))
      return FromSet(value);
    if (IsObject2(value))
      return FromObject10(value);
    if (IsValueType(value))
      return FromValue2(value);
    throw new Error("ValueClone: Unable to clone value");
  }

  // ../../node_modules/@sinclair/typebox/build/esm/value/create/create.mjs
  var ValueCreateError = class extends TypeBoxError {
    constructor(schema, message) {
      super(message);
      this.schema = schema;
    }
  };
  function FromDefault(value) {
    return IsFunction2(value) ? value() : Clone2(value);
  }
  function FromAny4(schema, references) {
    if (HasPropertyKey2(schema, "default")) {
      return FromDefault(schema.default);
    } else {
      return {};
    }
  }
  function FromArgument4(schema, references) {
    return {};
  }
  function FromArray10(schema, references) {
    if (schema.uniqueItems === true && !HasPropertyKey2(schema, "default")) {
      throw new ValueCreateError(schema, "Array with the uniqueItems constraint requires a default value");
    } else if ("contains" in schema && !HasPropertyKey2(schema, "default")) {
      throw new ValueCreateError(schema, "Array with the contains constraint requires a default value");
    } else if ("default" in schema) {
      return FromDefault(schema.default);
    } else if (schema.minItems !== void 0) {
      return Array.from({ length: schema.minItems }).map((item) => {
        return Visit7(schema.items, references);
      });
    } else {
      return [];
    }
  }
  function FromAsyncIterator6(schema, references) {
    if (HasPropertyKey2(schema, "default")) {
      return FromDefault(schema.default);
    } else {
      return async function* () {
      }();
    }
  }
  function FromBigInt4(schema, references) {
    if (HasPropertyKey2(schema, "default")) {
      return FromDefault(schema.default);
    } else {
      return BigInt(0);
    }
  }
  function FromBoolean4(schema, references) {
    if (HasPropertyKey2(schema, "default")) {
      return FromDefault(schema.default);
    } else {
      return false;
    }
  }
  function FromConstructor6(schema, references) {
    if (HasPropertyKey2(schema, "default")) {
      return FromDefault(schema.default);
    } else {
      const value = Visit7(schema.returns, references);
      if (typeof value === "object" && !Array.isArray(value)) {
        return class {
          constructor() {
            for (const [key, val] of Object.entries(value)) {
              const self = this;
              self[key] = val;
            }
          }
        };
      } else {
        return class {
        };
      }
    }
  }
  function FromDate5(schema, references) {
    if (HasPropertyKey2(schema, "default")) {
      return FromDefault(schema.default);
    } else if (schema.minimumTimestamp !== void 0) {
      return new Date(schema.minimumTimestamp);
    } else {
      return /* @__PURE__ */ new Date();
    }
  }
  function FromFunction6(schema, references) {
    if (HasPropertyKey2(schema, "default")) {
      return FromDefault(schema.default);
    } else {
      return () => Visit7(schema.returns, references);
    }
  }
  function FromImport3(schema, references) {
    const definitions = globalThis.Object.values(schema.$defs);
    const target = schema.$defs[schema.$ref];
    return Visit7(target, [...references, ...definitions]);
  }
  function FromInteger4(schema, references) {
    if (HasPropertyKey2(schema, "default")) {
      return FromDefault(schema.default);
    } else if (schema.minimum !== void 0) {
      return schema.minimum;
    } else {
      return 0;
    }
  }
  function FromIntersect11(schema, references) {
    if (HasPropertyKey2(schema, "default")) {
      return FromDefault(schema.default);
    } else {
      const value = schema.allOf.reduce((acc, schema2) => {
        const next = Visit7(schema2, references);
        return typeof next === "object" ? { ...acc, ...next } : next;
      }, {});
      if (!Check(schema, references, value))
        throw new ValueCreateError(schema, "Intersect produced invalid value. Consider using a default value.");
      return value;
    }
  }
  function FromIterator6(schema, references) {
    if (HasPropertyKey2(schema, "default")) {
      return FromDefault(schema.default);
    } else {
      return function* () {
      }();
    }
  }
  function FromLiteral5(schema, references) {
    if (HasPropertyKey2(schema, "default")) {
      return FromDefault(schema.default);
    } else {
      return schema.const;
    }
  }
  function FromNever4(schema, references) {
    if (HasPropertyKey2(schema, "default")) {
      return FromDefault(schema.default);
    } else {
      throw new ValueCreateError(schema, "Never types cannot be created. Consider using a default value.");
    }
  }
  function FromNot4(schema, references) {
    if (HasPropertyKey2(schema, "default")) {
      return FromDefault(schema.default);
    } else {
      throw new ValueCreateError(schema, "Not types must have a default value");
    }
  }
  function FromNull4(schema, references) {
    if (HasPropertyKey2(schema, "default")) {
      return FromDefault(schema.default);
    } else {
      return null;
    }
  }
  function FromNumber4(schema, references) {
    if (HasPropertyKey2(schema, "default")) {
      return FromDefault(schema.default);
    } else if (schema.minimum !== void 0) {
      return schema.minimum;
    } else {
      return 0;
    }
  }
  function FromObject11(schema, references) {
    if (HasPropertyKey2(schema, "default")) {
      return FromDefault(schema.default);
    } else {
      const required = new Set(schema.required);
      const Acc = {};
      for (const [key, subschema] of Object.entries(schema.properties)) {
        if (!required.has(key))
          continue;
        Acc[key] = Visit7(subschema, references);
      }
      return Acc;
    }
  }
  function FromPromise6(schema, references) {
    if (HasPropertyKey2(schema, "default")) {
      return FromDefault(schema.default);
    } else {
      return Promise.resolve(Visit7(schema.item, references));
    }
  }
  function FromRecord6(schema, references) {
    if (HasPropertyKey2(schema, "default")) {
      return FromDefault(schema.default);
    } else {
      return {};
    }
  }
  function FromRef7(schema, references) {
    if (HasPropertyKey2(schema, "default")) {
      return FromDefault(schema.default);
    } else {
      return Visit7(Deref(schema, references), references);
    }
  }
  function FromRegExp4(schema, references) {
    if (HasPropertyKey2(schema, "default")) {
      return FromDefault(schema.default);
    } else {
      throw new ValueCreateError(schema, "RegExp types cannot be created. Consider using a default value.");
    }
  }
  function FromString4(schema, references) {
    if (schema.pattern !== void 0) {
      if (!HasPropertyKey2(schema, "default")) {
        throw new ValueCreateError(schema, "String types with patterns must specify a default value");
      } else {
        return FromDefault(schema.default);
      }
    } else if (schema.format !== void 0) {
      if (!HasPropertyKey2(schema, "default")) {
        throw new ValueCreateError(schema, "String types with formats must specify a default value");
      } else {
        return FromDefault(schema.default);
      }
    } else {
      if (HasPropertyKey2(schema, "default")) {
        return FromDefault(schema.default);
      } else if (schema.minLength !== void 0) {
        return Array.from({ length: schema.minLength }).map(() => " ").join("");
      } else {
        return "";
      }
    }
  }
  function FromSymbol4(schema, references) {
    if (HasPropertyKey2(schema, "default")) {
      return FromDefault(schema.default);
    } else if ("value" in schema) {
      return Symbol.for(schema.value);
    } else {
      return Symbol();
    }
  }
  function FromTemplateLiteral6(schema, references) {
    if (HasPropertyKey2(schema, "default")) {
      return FromDefault(schema.default);
    }
    if (!IsTemplateLiteralFinite(schema))
      throw new ValueCreateError(schema, "Can only create template literals that produce a finite variants. Consider using a default value.");
    const generated = TemplateLiteralGenerate(schema);
    return generated[0];
  }
  function FromThis3(schema, references) {
    if (recursiveDepth++ > recursiveMaxDepth)
      throw new ValueCreateError(schema, "Cannot create recursive type as it appears possibly infinite. Consider using a default.");
    if (HasPropertyKey2(schema, "default")) {
      return FromDefault(schema.default);
    } else {
      return Visit7(Deref(schema, references), references);
    }
  }
  function FromTuple8(schema, references) {
    if (HasPropertyKey2(schema, "default")) {
      return FromDefault(schema.default);
    }
    if (schema.items === void 0) {
      return [];
    } else {
      return Array.from({ length: schema.minItems }).map((_, index) => Visit7(schema.items[index], references));
    }
  }
  function FromUndefined4(schema, references) {
    if (HasPropertyKey2(schema, "default")) {
      return FromDefault(schema.default);
    } else {
      return void 0;
    }
  }
  function FromUnion13(schema, references) {
    if (HasPropertyKey2(schema, "default")) {
      return FromDefault(schema.default);
    } else if (schema.anyOf.length === 0) {
      throw new Error("ValueCreate.Union: Cannot create Union with zero variants");
    } else {
      return Visit7(schema.anyOf[0], references);
    }
  }
  function FromUint8Array4(schema, references) {
    if (HasPropertyKey2(schema, "default")) {
      return FromDefault(schema.default);
    } else if (schema.minByteLength !== void 0) {
      return new Uint8Array(schema.minByteLength);
    } else {
      return new Uint8Array(0);
    }
  }
  function FromUnknown4(schema, references) {
    if (HasPropertyKey2(schema, "default")) {
      return FromDefault(schema.default);
    } else {
      return {};
    }
  }
  function FromVoid4(schema, references) {
    if (HasPropertyKey2(schema, "default")) {
      return FromDefault(schema.default);
    } else {
      return void 0;
    }
  }
  function FromKind3(schema, references) {
    if (HasPropertyKey2(schema, "default")) {
      return FromDefault(schema.default);
    } else {
      throw new Error("User defined types must specify a default value");
    }
  }
  function Visit7(schema, references) {
    const references_ = Pushref(schema, references);
    const schema_ = schema;
    switch (schema_[Kind]) {
      case "Any":
        return FromAny4(schema_, references_);
      case "Argument":
        return FromArgument4(schema_, references_);
      case "Array":
        return FromArray10(schema_, references_);
      case "AsyncIterator":
        return FromAsyncIterator6(schema_, references_);
      case "BigInt":
        return FromBigInt4(schema_, references_);
      case "Boolean":
        return FromBoolean4(schema_, references_);
      case "Constructor":
        return FromConstructor6(schema_, references_);
      case "Date":
        return FromDate5(schema_, references_);
      case "Function":
        return FromFunction6(schema_, references_);
      case "Import":
        return FromImport3(schema_, references_);
      case "Integer":
        return FromInteger4(schema_, references_);
      case "Intersect":
        return FromIntersect11(schema_, references_);
      case "Iterator":
        return FromIterator6(schema_, references_);
      case "Literal":
        return FromLiteral5(schema_, references_);
      case "Never":
        return FromNever4(schema_, references_);
      case "Not":
        return FromNot4(schema_, references_);
      case "Null":
        return FromNull4(schema_, references_);
      case "Number":
        return FromNumber4(schema_, references_);
      case "Object":
        return FromObject11(schema_, references_);
      case "Promise":
        return FromPromise6(schema_, references_);
      case "Record":
        return FromRecord6(schema_, references_);
      case "Ref":
        return FromRef7(schema_, references_);
      case "RegExp":
        return FromRegExp4(schema_, references_);
      case "String":
        return FromString4(schema_, references_);
      case "Symbol":
        return FromSymbol4(schema_, references_);
      case "TemplateLiteral":
        return FromTemplateLiteral6(schema_, references_);
      case "This":
        return FromThis3(schema_, references_);
      case "Tuple":
        return FromTuple8(schema_, references_);
      case "Undefined":
        return FromUndefined4(schema_, references_);
      case "Union":
        return FromUnion13(schema_, references_);
      case "Uint8Array":
        return FromUint8Array4(schema_, references_);
      case "Unknown":
        return FromUnknown4(schema_, references_);
      case "Void":
        return FromVoid4(schema_, references_);
      default:
        if (!type_exports2.Has(schema_[Kind]))
          throw new ValueCreateError(schema_, "Unknown type");
        return FromKind3(schema_, references_);
    }
  }
  var recursiveMaxDepth = 512;
  var recursiveDepth = 0;
  function Create2(...args) {
    recursiveDepth = 0;
    return args.length === 2 ? Visit7(args[0], args[1]) : Visit7(args[0], []);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/value/cast/cast.mjs
  var ValueCastError = class extends TypeBoxError {
    constructor(schema, message) {
      super(message);
      this.schema = schema;
    }
  };
  function ScoreUnion(schema, references, value) {
    if (schema[Kind] === "Object" && typeof value === "object" && !IsNull2(value)) {
      const object = schema;
      const keys = Object.getOwnPropertyNames(value);
      const entries = Object.entries(object.properties);
      return entries.reduce((acc, [key, schema2]) => {
        const literal = schema2[Kind] === "Literal" && schema2.const === value[key] ? 100 : 0;
        const checks = Check(schema2, references, value[key]) ? 10 : 0;
        const exists = keys.includes(key) ? 1 : 0;
        return acc + (literal + checks + exists);
      }, 0);
    } else if (schema[Kind] === "Union") {
      const schemas = schema.anyOf.map((schema2) => Deref(schema2, references));
      const scores = schemas.map((schema2) => ScoreUnion(schema2, references, value));
      return Math.max(...scores);
    } else {
      return Check(schema, references, value) ? 1 : 0;
    }
  }
  function SelectUnion(union, references, value) {
    const schemas = union.anyOf.map((schema) => Deref(schema, references));
    let [select, best] = [schemas[0], 0];
    for (const schema of schemas) {
      const score = ScoreUnion(schema, references, value);
      if (score > best) {
        select = schema;
        best = score;
      }
    }
    return select;
  }
  function CastUnion(union, references, value) {
    if ("default" in union) {
      return typeof value === "function" ? union.default : Clone2(union.default);
    } else {
      const schema = SelectUnion(union, references, value);
      return Cast(schema, references, value);
    }
  }
  function DefaultClone(schema, references, value) {
    return Check(schema, references, value) ? Clone2(value) : Create2(schema, references);
  }
  function Default(schema, references, value) {
    return Check(schema, references, value) ? value : Create2(schema, references);
  }
  function FromArray11(schema, references, value) {
    if (Check(schema, references, value))
      return Clone2(value);
    const created = IsArray2(value) ? Clone2(value) : Create2(schema, references);
    const minimum = IsNumber2(schema.minItems) && created.length < schema.minItems ? [...created, ...Array.from({ length: schema.minItems - created.length }, () => null)] : created;
    const maximum = IsNumber2(schema.maxItems) && minimum.length > schema.maxItems ? minimum.slice(0, schema.maxItems) : minimum;
    const casted = maximum.map((value2) => Visit8(schema.items, references, value2));
    if (schema.uniqueItems !== true)
      return casted;
    const unique = [...new Set(casted)];
    if (!Check(schema, references, unique))
      throw new ValueCastError(schema, "Array cast produced invalid data due to uniqueItems constraint");
    return unique;
  }
  function FromConstructor7(schema, references, value) {
    if (Check(schema, references, value))
      return Create2(schema, references);
    const required = new Set(schema.returns.required || []);
    const result = function() {
    };
    for (const [key, property] of Object.entries(schema.returns.properties)) {
      if (!required.has(key) && value.prototype[key] === void 0)
        continue;
      result.prototype[key] = Visit8(property, references, value.prototype[key]);
    }
    return result;
  }
  function FromImport4(schema, references, value) {
    const definitions = globalThis.Object.values(schema.$defs);
    const target = schema.$defs[schema.$ref];
    return Visit8(target, [...references, ...definitions], value);
  }
  function IntersectAssign(correct, value) {
    if (IsObject2(correct) && !IsObject2(value) || !IsObject2(correct) && IsObject2(value))
      return correct;
    if (!IsObject2(correct) || !IsObject2(value))
      return value;
    return globalThis.Object.getOwnPropertyNames(correct).reduce((result, key) => {
      const property = key in value ? IntersectAssign(correct[key], value[key]) : correct[key];
      return { ...result, [key]: property };
    }, {});
  }
  function FromIntersect12(schema, references, value) {
    if (Check(schema, references, value))
      return value;
    const correct = Create2(schema, references);
    const assigned = IntersectAssign(correct, value);
    return Check(schema, references, assigned) ? assigned : correct;
  }
  function FromNever5(schema, references, value) {
    throw new ValueCastError(schema, "Never types cannot be cast");
  }
  function FromObject12(schema, references, value) {
    if (Check(schema, references, value))
      return value;
    if (value === null || typeof value !== "object")
      return Create2(schema, references);
    const required = new Set(schema.required || []);
    const result = {};
    for (const [key, property] of Object.entries(schema.properties)) {
      if (!required.has(key) && value[key] === void 0)
        continue;
      result[key] = Visit8(property, references, value[key]);
    }
    if (typeof schema.additionalProperties === "object") {
      const propertyNames = Object.getOwnPropertyNames(schema.properties);
      for (const propertyName of Object.getOwnPropertyNames(value)) {
        if (propertyNames.includes(propertyName))
          continue;
        result[propertyName] = Visit8(schema.additionalProperties, references, value[propertyName]);
      }
    }
    return result;
  }
  function FromRecord7(schema, references, value) {
    if (Check(schema, references, value))
      return Clone2(value);
    if (value === null || typeof value !== "object" || Array.isArray(value) || value instanceof Date)
      return Create2(schema, references);
    const subschemaPropertyName = Object.getOwnPropertyNames(schema.patternProperties)[0];
    const subschema = schema.patternProperties[subschemaPropertyName];
    const result = {};
    for (const [propKey, propValue] of Object.entries(value)) {
      result[propKey] = Visit8(subschema, references, propValue);
    }
    return result;
  }
  function FromRef8(schema, references, value) {
    return Visit8(Deref(schema, references), references, value);
  }
  function FromThis4(schema, references, value) {
    return Visit8(Deref(schema, references), references, value);
  }
  function FromTuple9(schema, references, value) {
    if (Check(schema, references, value))
      return Clone2(value);
    if (!IsArray2(value))
      return Create2(schema, references);
    if (schema.items === void 0)
      return [];
    return schema.items.map((schema2, index) => Visit8(schema2, references, value[index]));
  }
  function FromUnion14(schema, references, value) {
    return Check(schema, references, value) ? Clone2(value) : CastUnion(schema, references, value);
  }
  function Visit8(schema, references, value) {
    const references_ = IsString2(schema.$id) ? Pushref(schema, references) : references;
    const schema_ = schema;
    switch (schema[Kind]) {
      // --------------------------------------------------------------
      // Structural
      // --------------------------------------------------------------
      case "Array":
        return FromArray11(schema_, references_, value);
      case "Constructor":
        return FromConstructor7(schema_, references_, value);
      case "Import":
        return FromImport4(schema_, references_, value);
      case "Intersect":
        return FromIntersect12(schema_, references_, value);
      case "Never":
        return FromNever5(schema_, references_, value);
      case "Object":
        return FromObject12(schema_, references_, value);
      case "Record":
        return FromRecord7(schema_, references_, value);
      case "Ref":
        return FromRef8(schema_, references_, value);
      case "This":
        return FromThis4(schema_, references_, value);
      case "Tuple":
        return FromTuple9(schema_, references_, value);
      case "Union":
        return FromUnion14(schema_, references_, value);
      // --------------------------------------------------------------
      // DefaultClone
      // --------------------------------------------------------------
      case "Date":
      case "Symbol":
      case "Uint8Array":
        return DefaultClone(schema, references, value);
      // --------------------------------------------------------------
      // Default
      // --------------------------------------------------------------
      default:
        return Default(schema_, references_, value);
    }
  }
  function Cast(...args) {
    return args.length === 3 ? Visit8(args[0], args[1], args[2]) : Visit8(args[0], [], args[1]);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/value/clean/clean.mjs
  function IsCheckable(schema) {
    return IsKind(schema) && schema[Kind] !== "Unsafe";
  }
  function FromArray12(schema, references, value) {
    if (!IsArray2(value))
      return value;
    return value.map((value2) => Visit9(schema.items, references, value2));
  }
  function FromImport5(schema, references, value) {
    const definitions = globalThis.Object.values(schema.$defs);
    const target = schema.$defs[schema.$ref];
    return Visit9(target, [...references, ...definitions], value);
  }
  function FromIntersect13(schema, references, value) {
    const unevaluatedProperties = schema.unevaluatedProperties;
    const intersections = schema.allOf.map((schema2) => Visit9(schema2, references, Clone2(value)));
    const composite = intersections.reduce((acc, value2) => IsObject2(value2) ? { ...acc, ...value2 } : value2, {});
    if (!IsObject2(value) || !IsObject2(composite) || !IsKind(unevaluatedProperties))
      return composite;
    const knownkeys = KeyOfPropertyKeys(schema);
    for (const key of Object.getOwnPropertyNames(value)) {
      if (knownkeys.includes(key))
        continue;
      if (Check(unevaluatedProperties, references, value[key])) {
        composite[key] = Visit9(unevaluatedProperties, references, value[key]);
      }
    }
    return composite;
  }
  function FromObject13(schema, references, value) {
    if (!IsObject2(value) || IsArray2(value))
      return value;
    const additionalProperties = schema.additionalProperties;
    for (const key of Object.getOwnPropertyNames(value)) {
      if (HasPropertyKey2(schema.properties, key)) {
        value[key] = Visit9(schema.properties[key], references, value[key]);
        continue;
      }
      if (IsKind(additionalProperties) && Check(additionalProperties, references, value[key])) {
        value[key] = Visit9(additionalProperties, references, value[key]);
        continue;
      }
      delete value[key];
    }
    return value;
  }
  function FromRecord8(schema, references, value) {
    if (!IsObject2(value))
      return value;
    const additionalProperties = schema.additionalProperties;
    const propertyKeys = Object.getOwnPropertyNames(value);
    const [propertyKey, propertySchema] = Object.entries(schema.patternProperties)[0];
    const propertyKeyTest = new RegExp(propertyKey);
    for (const key of propertyKeys) {
      if (propertyKeyTest.test(key)) {
        value[key] = Visit9(propertySchema, references, value[key]);
        continue;
      }
      if (IsKind(additionalProperties) && Check(additionalProperties, references, value[key])) {
        value[key] = Visit9(additionalProperties, references, value[key]);
        continue;
      }
      delete value[key];
    }
    return value;
  }
  function FromRef9(schema, references, value) {
    return Visit9(Deref(schema, references), references, value);
  }
  function FromThis5(schema, references, value) {
    return Visit9(Deref(schema, references), references, value);
  }
  function FromTuple10(schema, references, value) {
    if (!IsArray2(value))
      return value;
    if (IsUndefined2(schema.items))
      return [];
    const length = Math.min(value.length, schema.items.length);
    for (let i = 0; i < length; i++) {
      value[i] = Visit9(schema.items[i], references, value[i]);
    }
    return value.length > length ? value.slice(0, length) : value;
  }
  function FromUnion15(schema, references, value) {
    for (const inner of schema.anyOf) {
      if (IsCheckable(inner) && Check(inner, references, value)) {
        return Visit9(inner, references, value);
      }
    }
    return value;
  }
  function Visit9(schema, references, value) {
    const references_ = IsString2(schema.$id) ? Pushref(schema, references) : references;
    const schema_ = schema;
    switch (schema_[Kind]) {
      case "Array":
        return FromArray12(schema_, references_, value);
      case "Import":
        return FromImport5(schema_, references_, value);
      case "Intersect":
        return FromIntersect13(schema_, references_, value);
      case "Object":
        return FromObject13(schema_, references_, value);
      case "Record":
        return FromRecord8(schema_, references_, value);
      case "Ref":
        return FromRef9(schema_, references_, value);
      case "This":
        return FromThis5(schema_, references_, value);
      case "Tuple":
        return FromTuple10(schema_, references_, value);
      case "Union":
        return FromUnion15(schema_, references_, value);
      default:
        return value;
    }
  }
  function Clean(...args) {
    return args.length === 3 ? Visit9(args[0], args[1], args[2]) : Visit9(args[0], [], args[1]);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/value/convert/convert.mjs
  function IsStringNumeric(value) {
    return IsString2(value) && !isNaN(value) && !isNaN(parseFloat(value));
  }
  function IsValueToString(value) {
    return IsBigInt2(value) || IsBoolean2(value) || IsNumber2(value);
  }
  function IsValueTrue(value) {
    return value === true || IsNumber2(value) && value === 1 || IsBigInt2(value) && value === BigInt("1") || IsString2(value) && (value.toLowerCase() === "true" || value === "1");
  }
  function IsValueFalse(value) {
    return value === false || IsNumber2(value) && (value === 0 || Object.is(value, -0)) || IsBigInt2(value) && value === BigInt("0") || IsString2(value) && (value.toLowerCase() === "false" || value === "0" || value === "-0");
  }
  function IsTimeStringWithTimeZone(value) {
    return IsString2(value) && /^(?:[0-2]\d:[0-5]\d:[0-5]\d|23:59:60)(?:\.\d+)?(?:z|[+-]\d\d(?::?\d\d)?)$/i.test(value);
  }
  function IsTimeStringWithoutTimeZone(value) {
    return IsString2(value) && /^(?:[0-2]\d:[0-5]\d:[0-5]\d|23:59:60)?$/i.test(value);
  }
  function IsDateTimeStringWithTimeZone(value) {
    return IsString2(value) && /^\d\d\d\d-[0-1]\d-[0-3]\dt(?:[0-2]\d:[0-5]\d:[0-5]\d|23:59:60)(?:\.\d+)?(?:z|[+-]\d\d(?::?\d\d)?)$/i.test(value);
  }
  function IsDateTimeStringWithoutTimeZone(value) {
    return IsString2(value) && /^\d\d\d\d-[0-1]\d-[0-3]\dt(?:[0-2]\d:[0-5]\d:[0-5]\d|23:59:60)?$/i.test(value);
  }
  function IsDateString(value) {
    return IsString2(value) && /^\d\d\d\d-[0-1]\d-[0-3]\d$/i.test(value);
  }
  function TryConvertLiteralString(value, target) {
    const conversion = TryConvertString(value);
    return conversion === target ? conversion : value;
  }
  function TryConvertLiteralNumber(value, target) {
    const conversion = TryConvertNumber(value);
    return conversion === target ? conversion : value;
  }
  function TryConvertLiteralBoolean(value, target) {
    const conversion = TryConvertBoolean(value);
    return conversion === target ? conversion : value;
  }
  function TryConvertLiteral(schema, value) {
    return IsString2(schema.const) ? TryConvertLiteralString(value, schema.const) : IsNumber2(schema.const) ? TryConvertLiteralNumber(value, schema.const) : IsBoolean2(schema.const) ? TryConvertLiteralBoolean(value, schema.const) : value;
  }
  function TryConvertBoolean(value) {
    return IsValueTrue(value) ? true : IsValueFalse(value) ? false : value;
  }
  function TryConvertBigInt(value) {
    const truncateInteger = (value2) => value2.split(".")[0];
    return IsStringNumeric(value) ? BigInt(truncateInteger(value)) : IsNumber2(value) ? BigInt(Math.trunc(value)) : IsValueFalse(value) ? BigInt(0) : IsValueTrue(value) ? BigInt(1) : value;
  }
  function TryConvertString(value) {
    return IsSymbol2(value) && value.description !== void 0 ? value.description.toString() : IsValueToString(value) ? value.toString() : value;
  }
  function TryConvertNumber(value) {
    return IsStringNumeric(value) ? parseFloat(value) : IsValueTrue(value) ? 1 : IsValueFalse(value) ? 0 : value;
  }
  function TryConvertInteger(value) {
    return IsStringNumeric(value) ? parseInt(value) : IsNumber2(value) ? Math.trunc(value) : IsValueTrue(value) ? 1 : IsValueFalse(value) ? 0 : value;
  }
  function TryConvertNull(value) {
    return IsString2(value) && value.toLowerCase() === "null" ? null : value;
  }
  function TryConvertUndefined(value) {
    return IsString2(value) && value === "undefined" ? void 0 : value;
  }
  function TryConvertDate(value) {
    return IsDate2(value) ? value : IsNumber2(value) ? new Date(value) : IsValueTrue(value) ? /* @__PURE__ */ new Date(1) : IsValueFalse(value) ? /* @__PURE__ */ new Date(0) : IsStringNumeric(value) ? new Date(parseInt(value)) : IsTimeStringWithoutTimeZone(value) ? /* @__PURE__ */ new Date(`1970-01-01T${value}.000Z`) : IsTimeStringWithTimeZone(value) ? /* @__PURE__ */ new Date(`1970-01-01T${value}`) : IsDateTimeStringWithoutTimeZone(value) ? /* @__PURE__ */ new Date(`${value}.000Z`) : IsDateTimeStringWithTimeZone(value) ? new Date(value) : IsDateString(value) ? /* @__PURE__ */ new Date(`${value}T00:00:00.000Z`) : value;
  }
  function Default2(value) {
    return value;
  }
  function FromArray13(schema, references, value) {
    const elements = IsArray2(value) ? value : [value];
    return elements.map((element) => Visit10(schema.items, references, element));
  }
  function FromBigInt5(schema, references, value) {
    return TryConvertBigInt(value);
  }
  function FromBoolean5(schema, references, value) {
    return TryConvertBoolean(value);
  }
  function FromDate6(schema, references, value) {
    return TryConvertDate(value);
  }
  function FromImport6(schema, references, value) {
    const definitions = globalThis.Object.values(schema.$defs);
    const target = schema.$defs[schema.$ref];
    return Visit10(target, [...references, ...definitions], value);
  }
  function FromInteger5(schema, references, value) {
    return TryConvertInteger(value);
  }
  function FromIntersect14(schema, references, value) {
    return schema.allOf.reduce((value2, schema2) => Visit10(schema2, references, value2), value);
  }
  function FromLiteral6(schema, references, value) {
    return TryConvertLiteral(schema, value);
  }
  function FromNull5(schema, references, value) {
    return TryConvertNull(value);
  }
  function FromNumber5(schema, references, value) {
    return TryConvertNumber(value);
  }
  function FromObject14(schema, references, value) {
    if (!IsObject2(value) || IsArray2(value))
      return value;
    for (const propertyKey of Object.getOwnPropertyNames(schema.properties)) {
      if (!HasPropertyKey2(value, propertyKey))
        continue;
      value[propertyKey] = Visit10(schema.properties[propertyKey], references, value[propertyKey]);
    }
    return value;
  }
  function FromRecord9(schema, references, value) {
    const isConvertable = IsObject2(value) && !IsArray2(value);
    if (!isConvertable)
      return value;
    const propertyKey = Object.getOwnPropertyNames(schema.patternProperties)[0];
    const property = schema.patternProperties[propertyKey];
    for (const [propKey, propValue] of Object.entries(value)) {
      value[propKey] = Visit10(property, references, propValue);
    }
    return value;
  }
  function FromRef10(schema, references, value) {
    return Visit10(Deref(schema, references), references, value);
  }
  function FromString5(schema, references, value) {
    return TryConvertString(value);
  }
  function FromSymbol5(schema, references, value) {
    return IsString2(value) || IsNumber2(value) ? Symbol(value) : value;
  }
  function FromThis6(schema, references, value) {
    return Visit10(Deref(schema, references), references, value);
  }
  function FromTuple11(schema, references, value) {
    const isConvertable = IsArray2(value) && !IsUndefined2(schema.items);
    if (!isConvertable)
      return value;
    return value.map((value2, index) => {
      return index < schema.items.length ? Visit10(schema.items[index], references, value2) : value2;
    });
  }
  function FromUndefined5(schema, references, value) {
    return TryConvertUndefined(value);
  }
  function FromUnion16(schema, references, value) {
    for (const subschema of schema.anyOf) {
      if (Check(subschema, references, value)) {
        return value;
      }
    }
    for (const subschema of schema.anyOf) {
      const converted = Visit10(subschema, references, Clone2(value));
      if (!Check(subschema, references, converted))
        continue;
      return converted;
    }
    return value;
  }
  function Visit10(schema, references, value) {
    const references_ = Pushref(schema, references);
    const schema_ = schema;
    switch (schema[Kind]) {
      case "Array":
        return FromArray13(schema_, references_, value);
      case "BigInt":
        return FromBigInt5(schema_, references_, value);
      case "Boolean":
        return FromBoolean5(schema_, references_, value);
      case "Date":
        return FromDate6(schema_, references_, value);
      case "Import":
        return FromImport6(schema_, references_, value);
      case "Integer":
        return FromInteger5(schema_, references_, value);
      case "Intersect":
        return FromIntersect14(schema_, references_, value);
      case "Literal":
        return FromLiteral6(schema_, references_, value);
      case "Null":
        return FromNull5(schema_, references_, value);
      case "Number":
        return FromNumber5(schema_, references_, value);
      case "Object":
        return FromObject14(schema_, references_, value);
      case "Record":
        return FromRecord9(schema_, references_, value);
      case "Ref":
        return FromRef10(schema_, references_, value);
      case "String":
        return FromString5(schema_, references_, value);
      case "Symbol":
        return FromSymbol5(schema_, references_, value);
      case "This":
        return FromThis6(schema_, references_, value);
      case "Tuple":
        return FromTuple11(schema_, references_, value);
      case "Undefined":
        return FromUndefined5(schema_, references_, value);
      case "Union":
        return FromUnion16(schema_, references_, value);
      default:
        return Default2(value);
    }
  }
  function Convert(...args) {
    return args.length === 3 ? Visit10(args[0], args[1], args[2]) : Visit10(args[0], [], args[1]);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/value/transform/decode.mjs
  var TransformDecodeCheckError = class extends TypeBoxError {
    constructor(schema, value, error) {
      super(`Unable to decode value as it does not match the expected schema`);
      this.schema = schema;
      this.value = value;
      this.error = error;
    }
  };
  var TransformDecodeError = class extends TypeBoxError {
    constructor(schema, path, value, error) {
      super(error instanceof Error ? error.message : "Unknown error");
      this.schema = schema;
      this.path = path;
      this.value = value;
      this.error = error;
    }
  };
  function Default3(schema, path, value) {
    try {
      return IsTransform(schema) ? schema[TransformKind].Decode(value) : value;
    } catch (error) {
      throw new TransformDecodeError(schema, path, value, error);
    }
  }
  function FromArray14(schema, references, path, value) {
    return IsArray2(value) ? Default3(schema, path, value.map((value2, index) => Visit11(schema.items, references, `${path}/${index}`, value2))) : Default3(schema, path, value);
  }
  function FromIntersect15(schema, references, path, value) {
    if (!IsObject2(value) || IsValueType(value))
      return Default3(schema, path, value);
    const knownEntries = KeyOfPropertyEntries(schema);
    const knownKeys = knownEntries.map((entry) => entry[0]);
    const knownProperties = { ...value };
    for (const [knownKey, knownSchema] of knownEntries)
      if (knownKey in knownProperties) {
        knownProperties[knownKey] = Visit11(knownSchema, references, `${path}/${knownKey}`, knownProperties[knownKey]);
      }
    if (!IsTransform(schema.unevaluatedProperties)) {
      return Default3(schema, path, knownProperties);
    }
    const unknownKeys = Object.getOwnPropertyNames(knownProperties);
    const unevaluatedProperties = schema.unevaluatedProperties;
    const unknownProperties = { ...knownProperties };
    for (const key of unknownKeys)
      if (!knownKeys.includes(key)) {
        unknownProperties[key] = Default3(unevaluatedProperties, `${path}/${key}`, unknownProperties[key]);
      }
    return Default3(schema, path, unknownProperties);
  }
  function FromImport7(schema, references, path, value) {
    const additional = globalThis.Object.values(schema.$defs);
    const target = schema.$defs[schema.$ref];
    const result = Visit11(target, [...references, ...additional], path, value);
    return Default3(schema, path, result);
  }
  function FromNot5(schema, references, path, value) {
    return Default3(schema, path, Visit11(schema.not, references, path, value));
  }
  function FromObject15(schema, references, path, value) {
    if (!IsObject2(value))
      return Default3(schema, path, value);
    const knownKeys = KeyOfPropertyKeys(schema);
    const knownProperties = { ...value };
    for (const key of knownKeys) {
      if (!HasPropertyKey2(knownProperties, key))
        continue;
      if (IsUndefined2(knownProperties[key]) && (!IsUndefined3(schema.properties[key]) || TypeSystemPolicy.IsExactOptionalProperty(knownProperties, key)))
        continue;
      knownProperties[key] = Visit11(schema.properties[key], references, `${path}/${key}`, knownProperties[key]);
    }
    if (!IsSchema(schema.additionalProperties)) {
      return Default3(schema, path, knownProperties);
    }
    const unknownKeys = Object.getOwnPropertyNames(knownProperties);
    const additionalProperties = schema.additionalProperties;
    const unknownProperties = { ...knownProperties };
    for (const key of unknownKeys)
      if (!knownKeys.includes(key)) {
        unknownProperties[key] = Default3(additionalProperties, `${path}/${key}`, unknownProperties[key]);
      }
    return Default3(schema, path, unknownProperties);
  }
  function FromRecord10(schema, references, path, value) {
    if (!IsObject2(value))
      return Default3(schema, path, value);
    const pattern = Object.getOwnPropertyNames(schema.patternProperties)[0];
    const knownKeys = new RegExp(pattern);
    const knownProperties = { ...value };
    for (const key of Object.getOwnPropertyNames(value))
      if (knownKeys.test(key)) {
        knownProperties[key] = Visit11(schema.patternProperties[pattern], references, `${path}/${key}`, knownProperties[key]);
      }
    if (!IsSchema(schema.additionalProperties)) {
      return Default3(schema, path, knownProperties);
    }
    const unknownKeys = Object.getOwnPropertyNames(knownProperties);
    const additionalProperties = schema.additionalProperties;
    const unknownProperties = { ...knownProperties };
    for (const key of unknownKeys)
      if (!knownKeys.test(key)) {
        unknownProperties[key] = Default3(additionalProperties, `${path}/${key}`, unknownProperties[key]);
      }
    return Default3(schema, path, unknownProperties);
  }
  function FromRef11(schema, references, path, value) {
    const target = Deref(schema, references);
    return Default3(schema, path, Visit11(target, references, path, value));
  }
  function FromThis7(schema, references, path, value) {
    const target = Deref(schema, references);
    return Default3(schema, path, Visit11(target, references, path, value));
  }
  function FromTuple12(schema, references, path, value) {
    return IsArray2(value) && IsArray2(schema.items) ? Default3(schema, path, schema.items.map((schema2, index) => Visit11(schema2, references, `${path}/${index}`, value[index]))) : Default3(schema, path, value);
  }
  function FromUnion17(schema, references, path, value) {
    for (const subschema of schema.anyOf) {
      if (!Check(subschema, references, value))
        continue;
      const decoded = Visit11(subschema, references, path, value);
      return Default3(schema, path, decoded);
    }
    return Default3(schema, path, value);
  }
  function Visit11(schema, references, path, value) {
    const references_ = Pushref(schema, references);
    const schema_ = schema;
    switch (schema[Kind]) {
      case "Array":
        return FromArray14(schema_, references_, path, value);
      case "Import":
        return FromImport7(schema_, references_, path, value);
      case "Intersect":
        return FromIntersect15(schema_, references_, path, value);
      case "Not":
        return FromNot5(schema_, references_, path, value);
      case "Object":
        return FromObject15(schema_, references_, path, value);
      case "Record":
        return FromRecord10(schema_, references_, path, value);
      case "Ref":
        return FromRef11(schema_, references_, path, value);
      case "Symbol":
        return Default3(schema_, path, value);
      case "This":
        return FromThis7(schema_, references_, path, value);
      case "Tuple":
        return FromTuple12(schema_, references_, path, value);
      case "Union":
        return FromUnion17(schema_, references_, path, value);
      default:
        return Default3(schema_, path, value);
    }
  }
  function TransformDecode(schema, references, value) {
    return Visit11(schema, references, "", value);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/value/transform/encode.mjs
  var TransformEncodeCheckError = class extends TypeBoxError {
    constructor(schema, value, error) {
      super(`The encoded value does not match the expected schema`);
      this.schema = schema;
      this.value = value;
      this.error = error;
    }
  };
  var TransformEncodeError = class extends TypeBoxError {
    constructor(schema, path, value, error) {
      super(`${error instanceof Error ? error.message : "Unknown error"}`);
      this.schema = schema;
      this.path = path;
      this.value = value;
      this.error = error;
    }
  };
  function Default4(schema, path, value) {
    try {
      return IsTransform(schema) ? schema[TransformKind].Encode(value) : value;
    } catch (error) {
      throw new TransformEncodeError(schema, path, value, error);
    }
  }
  function FromArray15(schema, references, path, value) {
    const defaulted = Default4(schema, path, value);
    return IsArray2(defaulted) ? defaulted.map((value2, index) => Visit12(schema.items, references, `${path}/${index}`, value2)) : defaulted;
  }
  function FromImport8(schema, references, path, value) {
    const additional = globalThis.Object.values(schema.$defs);
    const target = schema.$defs[schema.$ref];
    const result = Default4(schema, path, value);
    return Visit12(target, [...references, ...additional], path, result);
  }
  function FromIntersect16(schema, references, path, value) {
    const defaulted = Default4(schema, path, value);
    if (!IsObject2(value) || IsValueType(value))
      return defaulted;
    const knownEntries = KeyOfPropertyEntries(schema);
    const knownKeys = knownEntries.map((entry) => entry[0]);
    const knownProperties = { ...defaulted };
    for (const [knownKey, knownSchema] of knownEntries)
      if (knownKey in knownProperties) {
        knownProperties[knownKey] = Visit12(knownSchema, references, `${path}/${knownKey}`, knownProperties[knownKey]);
      }
    if (!IsTransform(schema.unevaluatedProperties)) {
      return knownProperties;
    }
    const unknownKeys = Object.getOwnPropertyNames(knownProperties);
    const unevaluatedProperties = schema.unevaluatedProperties;
    const properties = { ...knownProperties };
    for (const key of unknownKeys)
      if (!knownKeys.includes(key)) {
        properties[key] = Default4(unevaluatedProperties, `${path}/${key}`, properties[key]);
      }
    return properties;
  }
  function FromNot6(schema, references, path, value) {
    return Default4(schema.not, path, Default4(schema, path, value));
  }
  function FromObject16(schema, references, path, value) {
    const defaulted = Default4(schema, path, value);
    if (!IsObject2(defaulted))
      return defaulted;
    const knownKeys = KeyOfPropertyKeys(schema);
    const knownProperties = { ...defaulted };
    for (const key of knownKeys) {
      if (!HasPropertyKey2(knownProperties, key))
        continue;
      if (IsUndefined2(knownProperties[key]) && (!IsUndefined3(schema.properties[key]) || TypeSystemPolicy.IsExactOptionalProperty(knownProperties, key)))
        continue;
      knownProperties[key] = Visit12(schema.properties[key], references, `${path}/${key}`, knownProperties[key]);
    }
    if (!IsSchema(schema.additionalProperties)) {
      return knownProperties;
    }
    const unknownKeys = Object.getOwnPropertyNames(knownProperties);
    const additionalProperties = schema.additionalProperties;
    const properties = { ...knownProperties };
    for (const key of unknownKeys)
      if (!knownKeys.includes(key)) {
        properties[key] = Default4(additionalProperties, `${path}/${key}`, properties[key]);
      }
    return properties;
  }
  function FromRecord11(schema, references, path, value) {
    const defaulted = Default4(schema, path, value);
    if (!IsObject2(value))
      return defaulted;
    const pattern = Object.getOwnPropertyNames(schema.patternProperties)[0];
    const knownKeys = new RegExp(pattern);
    const knownProperties = { ...defaulted };
    for (const key of Object.getOwnPropertyNames(value))
      if (knownKeys.test(key)) {
        knownProperties[key] = Visit12(schema.patternProperties[pattern], references, `${path}/${key}`, knownProperties[key]);
      }
    if (!IsSchema(schema.additionalProperties)) {
      return knownProperties;
    }
    const unknownKeys = Object.getOwnPropertyNames(knownProperties);
    const additionalProperties = schema.additionalProperties;
    const properties = { ...knownProperties };
    for (const key of unknownKeys)
      if (!knownKeys.test(key)) {
        properties[key] = Default4(additionalProperties, `${path}/${key}`, properties[key]);
      }
    return properties;
  }
  function FromRef12(schema, references, path, value) {
    const target = Deref(schema, references);
    const resolved = Visit12(target, references, path, value);
    return Default4(schema, path, resolved);
  }
  function FromThis8(schema, references, path, value) {
    const target = Deref(schema, references);
    const resolved = Visit12(target, references, path, value);
    return Default4(schema, path, resolved);
  }
  function FromTuple13(schema, references, path, value) {
    const value1 = Default4(schema, path, value);
    return IsArray2(schema.items) ? schema.items.map((schema2, index) => Visit12(schema2, references, `${path}/${index}`, value1[index])) : [];
  }
  function FromUnion18(schema, references, path, value) {
    for (const subschema of schema.anyOf) {
      if (!Check(subschema, references, value))
        continue;
      const value1 = Visit12(subschema, references, path, value);
      return Default4(schema, path, value1);
    }
    for (const subschema of schema.anyOf) {
      const value1 = Visit12(subschema, references, path, value);
      if (!Check(schema, references, value1))
        continue;
      return Default4(schema, path, value1);
    }
    return Default4(schema, path, value);
  }
  function Visit12(schema, references, path, value) {
    const references_ = Pushref(schema, references);
    const schema_ = schema;
    switch (schema[Kind]) {
      case "Array":
        return FromArray15(schema_, references_, path, value);
      case "Import":
        return FromImport8(schema_, references_, path, value);
      case "Intersect":
        return FromIntersect16(schema_, references_, path, value);
      case "Not":
        return FromNot6(schema_, references_, path, value);
      case "Object":
        return FromObject16(schema_, references_, path, value);
      case "Record":
        return FromRecord11(schema_, references_, path, value);
      case "Ref":
        return FromRef12(schema_, references_, path, value);
      case "This":
        return FromThis8(schema_, references_, path, value);
      case "Tuple":
        return FromTuple13(schema_, references_, path, value);
      case "Union":
        return FromUnion18(schema_, references_, path, value);
      default:
        return Default4(schema_, path, value);
    }
  }
  function TransformEncode(schema, references, value) {
    return Visit12(schema, references, "", value);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/value/transform/has.mjs
  function FromArray16(schema, references) {
    return IsTransform(schema) || Visit13(schema.items, references);
  }
  function FromAsyncIterator7(schema, references) {
    return IsTransform(schema) || Visit13(schema.items, references);
  }
  function FromConstructor8(schema, references) {
    return IsTransform(schema) || Visit13(schema.returns, references) || schema.parameters.some((schema2) => Visit13(schema2, references));
  }
  function FromFunction7(schema, references) {
    return IsTransform(schema) || Visit13(schema.returns, references) || schema.parameters.some((schema2) => Visit13(schema2, references));
  }
  function FromIntersect17(schema, references) {
    return IsTransform(schema) || IsTransform(schema.unevaluatedProperties) || schema.allOf.some((schema2) => Visit13(schema2, references));
  }
  function FromImport9(schema, references) {
    const additional = globalThis.Object.getOwnPropertyNames(schema.$defs).reduce((result, key) => [...result, schema.$defs[key]], []);
    const target = schema.$defs[schema.$ref];
    return IsTransform(schema) || Visit13(target, [...additional, ...references]);
  }
  function FromIterator7(schema, references) {
    return IsTransform(schema) || Visit13(schema.items, references);
  }
  function FromNot7(schema, references) {
    return IsTransform(schema) || Visit13(schema.not, references);
  }
  function FromObject17(schema, references) {
    return IsTransform(schema) || Object.values(schema.properties).some((schema2) => Visit13(schema2, references)) || IsSchema(schema.additionalProperties) && Visit13(schema.additionalProperties, references);
  }
  function FromPromise7(schema, references) {
    return IsTransform(schema) || Visit13(schema.item, references);
  }
  function FromRecord12(schema, references) {
    const pattern = Object.getOwnPropertyNames(schema.patternProperties)[0];
    const property = schema.patternProperties[pattern];
    return IsTransform(schema) || Visit13(property, references) || IsSchema(schema.additionalProperties) && IsTransform(schema.additionalProperties);
  }
  function FromRef13(schema, references) {
    if (IsTransform(schema))
      return true;
    return Visit13(Deref(schema, references), references);
  }
  function FromThis9(schema, references) {
    if (IsTransform(schema))
      return true;
    return Visit13(Deref(schema, references), references);
  }
  function FromTuple14(schema, references) {
    return IsTransform(schema) || !IsUndefined2(schema.items) && schema.items.some((schema2) => Visit13(schema2, references));
  }
  function FromUnion19(schema, references) {
    return IsTransform(schema) || schema.anyOf.some((schema2) => Visit13(schema2, references));
  }
  function Visit13(schema, references) {
    const references_ = Pushref(schema, references);
    const schema_ = schema;
    if (schema.$id && visited.has(schema.$id))
      return false;
    if (schema.$id)
      visited.add(schema.$id);
    switch (schema[Kind]) {
      case "Array":
        return FromArray16(schema_, references_);
      case "AsyncIterator":
        return FromAsyncIterator7(schema_, references_);
      case "Constructor":
        return FromConstructor8(schema_, references_);
      case "Function":
        return FromFunction7(schema_, references_);
      case "Import":
        return FromImport9(schema_, references_);
      case "Intersect":
        return FromIntersect17(schema_, references_);
      case "Iterator":
        return FromIterator7(schema_, references_);
      case "Not":
        return FromNot7(schema_, references_);
      case "Object":
        return FromObject17(schema_, references_);
      case "Promise":
        return FromPromise7(schema_, references_);
      case "Record":
        return FromRecord12(schema_, references_);
      case "Ref":
        return FromRef13(schema_, references_);
      case "This":
        return FromThis9(schema_, references_);
      case "Tuple":
        return FromTuple14(schema_, references_);
      case "Union":
        return FromUnion19(schema_, references_);
      default:
        return IsTransform(schema);
    }
  }
  var visited = /* @__PURE__ */ new Set();
  function HasTransform(schema, references) {
    visited.clear();
    return Visit13(schema, references);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/value/decode/decode.mjs
  function Decode(...args) {
    const [schema, references, value] = args.length === 3 ? [args[0], args[1], args[2]] : [args[0], [], args[1]];
    if (!Check(schema, references, value))
      throw new TransformDecodeCheckError(schema, value, Errors(schema, references, value).First());
    return HasTransform(schema, references) ? TransformDecode(schema, references, value) : value;
  }

  // ../../node_modules/@sinclair/typebox/build/esm/value/default/default.mjs
  function ValueOrDefault(schema, value) {
    const defaultValue = HasPropertyKey2(schema, "default") ? schema.default : void 0;
    const clone = IsFunction2(defaultValue) ? defaultValue() : Clone2(defaultValue);
    return IsUndefined2(value) ? clone : IsObject2(value) && IsObject2(clone) ? Object.assign(clone, value) : value;
  }
  function HasDefaultProperty(schema) {
    return IsKind(schema) && "default" in schema;
  }
  function FromArray17(schema, references, value) {
    if (IsArray2(value)) {
      for (let i = 0; i < value.length; i++) {
        value[i] = Visit14(schema.items, references, value[i]);
      }
      return value;
    }
    const defaulted = ValueOrDefault(schema, value);
    if (!IsArray2(defaulted))
      return defaulted;
    for (let i = 0; i < defaulted.length; i++) {
      defaulted[i] = Visit14(schema.items, references, defaulted[i]);
    }
    return defaulted;
  }
  function FromDate7(schema, references, value) {
    return IsDate2(value) ? value : ValueOrDefault(schema, value);
  }
  function FromImport10(schema, references, value) {
    const definitions = globalThis.Object.values(schema.$defs);
    const target = schema.$defs[schema.$ref];
    return Visit14(target, [...references, ...definitions], value);
  }
  function FromIntersect18(schema, references, value) {
    const defaulted = ValueOrDefault(schema, value);
    return schema.allOf.reduce((acc, schema2) => {
      const next = Visit14(schema2, references, defaulted);
      return IsObject2(next) ? { ...acc, ...next } : next;
    }, {});
  }
  function FromObject18(schema, references, value) {
    const defaulted = ValueOrDefault(schema, value);
    if (!IsObject2(defaulted))
      return defaulted;
    const knownPropertyKeys = Object.getOwnPropertyNames(schema.properties);
    for (const key of knownPropertyKeys) {
      const propertyValue = Visit14(schema.properties[key], references, defaulted[key]);
      if (IsUndefined2(propertyValue))
        continue;
      defaulted[key] = Visit14(schema.properties[key], references, defaulted[key]);
    }
    if (!HasDefaultProperty(schema.additionalProperties))
      return defaulted;
    for (const key of Object.getOwnPropertyNames(defaulted)) {
      if (knownPropertyKeys.includes(key))
        continue;
      defaulted[key] = Visit14(schema.additionalProperties, references, defaulted[key]);
    }
    return defaulted;
  }
  function FromRecord13(schema, references, value) {
    const defaulted = ValueOrDefault(schema, value);
    if (!IsObject2(defaulted))
      return defaulted;
    const additionalPropertiesSchema = schema.additionalProperties;
    const [propertyKeyPattern, propertySchema] = Object.entries(schema.patternProperties)[0];
    const knownPropertyKey = new RegExp(propertyKeyPattern);
    for (const key of Object.getOwnPropertyNames(defaulted)) {
      if (!(knownPropertyKey.test(key) && HasDefaultProperty(propertySchema)))
        continue;
      defaulted[key] = Visit14(propertySchema, references, defaulted[key]);
    }
    if (!HasDefaultProperty(additionalPropertiesSchema))
      return defaulted;
    for (const key of Object.getOwnPropertyNames(defaulted)) {
      if (knownPropertyKey.test(key))
        continue;
      defaulted[key] = Visit14(additionalPropertiesSchema, references, defaulted[key]);
    }
    return defaulted;
  }
  function FromRef14(schema, references, value) {
    return Visit14(Deref(schema, references), references, ValueOrDefault(schema, value));
  }
  function FromThis10(schema, references, value) {
    return Visit14(Deref(schema, references), references, value);
  }
  function FromTuple15(schema, references, value) {
    const defaulted = ValueOrDefault(schema, value);
    if (!IsArray2(defaulted) || IsUndefined2(schema.items))
      return defaulted;
    const [items, max] = [schema.items, Math.max(schema.items.length, defaulted.length)];
    for (let i = 0; i < max; i++) {
      if (i < items.length)
        defaulted[i] = Visit14(items[i], references, defaulted[i]);
    }
    return defaulted;
  }
  function FromUnion20(schema, references, value) {
    const defaulted = ValueOrDefault(schema, value);
    for (const inner of schema.anyOf) {
      const result = Visit14(inner, references, Clone2(defaulted));
      if (Check(inner, references, result)) {
        return result;
      }
    }
    return defaulted;
  }
  function Visit14(schema, references, value) {
    const references_ = Pushref(schema, references);
    const schema_ = schema;
    switch (schema_[Kind]) {
      case "Array":
        return FromArray17(schema_, references_, value);
      case "Date":
        return FromDate7(schema_, references_, value);
      case "Import":
        return FromImport10(schema_, references_, value);
      case "Intersect":
        return FromIntersect18(schema_, references_, value);
      case "Object":
        return FromObject18(schema_, references_, value);
      case "Record":
        return FromRecord13(schema_, references_, value);
      case "Ref":
        return FromRef14(schema_, references_, value);
      case "This":
        return FromThis10(schema_, references_, value);
      case "Tuple":
        return FromTuple15(schema_, references_, value);
      case "Union":
        return FromUnion20(schema_, references_, value);
      default:
        return ValueOrDefault(schema_, value);
    }
  }
  function Default5(...args) {
    return args.length === 3 ? Visit14(args[0], args[1], args[2]) : Visit14(args[0], [], args[1]);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/value/pointer/pointer.mjs
  var pointer_exports = {};
  __export(pointer_exports, {
    Delete: () => Delete3,
    Format: () => Format,
    Get: () => Get3,
    Has: () => Has3,
    Set: () => Set4,
    ValuePointerRootDeleteError: () => ValuePointerRootDeleteError,
    ValuePointerRootSetError: () => ValuePointerRootSetError
  });
  var ValuePointerRootSetError = class extends TypeBoxError {
    constructor(value, path, update) {
      super("Cannot set root value");
      this.value = value;
      this.path = path;
      this.update = update;
    }
  };
  var ValuePointerRootDeleteError = class extends TypeBoxError {
    constructor(value, path) {
      super("Cannot delete root value");
      this.value = value;
      this.path = path;
    }
  };
  function Escape2(component) {
    return component.indexOf("~") === -1 ? component : component.replace(/~1/g, "/").replace(/~0/g, "~");
  }
  function* Format(pointer) {
    if (pointer === "")
      return;
    let [start, end] = [0, 0];
    for (let i = 0; i < pointer.length; i++) {
      const char = pointer.charAt(i);
      if (char === "/") {
        if (i === 0) {
          start = i + 1;
        } else {
          end = i;
          yield Escape2(pointer.slice(start, end));
          start = i + 1;
        }
      } else {
        end = i;
      }
    }
    yield Escape2(pointer.slice(start));
  }
  function Set4(value, pointer, update) {
    if (pointer === "")
      throw new ValuePointerRootSetError(value, pointer, update);
    let [owner, next, key] = [null, value, ""];
    for (const component of Format(pointer)) {
      if (next[component] === void 0)
        next[component] = {};
      owner = next;
      next = next[component];
      key = component;
    }
    owner[key] = update;
  }
  function Delete3(value, pointer) {
    if (pointer === "")
      throw new ValuePointerRootDeleteError(value, pointer);
    let [owner, next, key] = [null, value, ""];
    for (const component of Format(pointer)) {
      if (next[component] === void 0 || next[component] === null)
        return;
      owner = next;
      next = next[component];
      key = component;
    }
    if (Array.isArray(owner)) {
      const index = parseInt(key);
      owner.splice(index, 1);
    } else {
      delete owner[key];
    }
  }
  function Has3(value, pointer) {
    if (pointer === "")
      return true;
    let [owner, next, key] = [null, value, ""];
    for (const component of Format(pointer)) {
      if (next[component] === void 0)
        return false;
      owner = next;
      next = next[component];
      key = component;
    }
    return Object.getOwnPropertyNames(owner).includes(key);
  }
  function Get3(value, pointer) {
    if (pointer === "")
      return value;
    let current = value;
    for (const component of Format(pointer)) {
      if (current[component] === void 0)
        return void 0;
      current = current[component];
    }
    return current;
  }

  // ../../node_modules/@sinclair/typebox/build/esm/value/equal/equal.mjs
  function ObjectType3(left, right) {
    if (!IsObject2(right))
      return false;
    const leftKeys = [...Object.keys(left), ...Object.getOwnPropertySymbols(left)];
    const rightKeys = [...Object.keys(right), ...Object.getOwnPropertySymbols(right)];
    if (leftKeys.length !== rightKeys.length)
      return false;
    return leftKeys.every((key) => Equal(left[key], right[key]));
  }
  function DateType3(left, right) {
    return IsDate2(right) && left.getTime() === right.getTime();
  }
  function ArrayType3(left, right) {
    if (!IsArray2(right) || left.length !== right.length)
      return false;
    return left.every((value, index) => Equal(value, right[index]));
  }
  function TypedArrayType(left, right) {
    if (!IsTypedArray(right) || left.length !== right.length || Object.getPrototypeOf(left).constructor.name !== Object.getPrototypeOf(right).constructor.name)
      return false;
    return left.every((value, index) => Equal(value, right[index]));
  }
  function ValueType(left, right) {
    return left === right;
  }
  function Equal(left, right) {
    if (IsDate2(left))
      return DateType3(left, right);
    if (IsTypedArray(left))
      return TypedArrayType(left, right);
    if (IsArray2(left))
      return ArrayType3(left, right);
    if (IsObject2(left))
      return ObjectType3(left, right);
    if (IsValueType(left))
      return ValueType(left, right);
    throw new Error("ValueEquals: Unable to compare value");
  }

  // ../../node_modules/@sinclair/typebox/build/esm/value/delta/delta.mjs
  var Insert = Object2({
    type: Literal("insert"),
    path: String2(),
    value: Unknown()
  });
  var Update = Object2({
    type: Literal("update"),
    path: String2(),
    value: Unknown()
  });
  var Delete4 = Object2({
    type: Literal("delete"),
    path: String2()
  });
  var Edit = Union([Insert, Update, Delete4]);
  var ValueDiffError = class extends TypeBoxError {
    constructor(value, message) {
      super(message);
      this.value = value;
    }
  };
  function CreateUpdate(path, value) {
    return { type: "update", path, value };
  }
  function CreateInsert(path, value) {
    return { type: "insert", path, value };
  }
  function CreateDelete(path) {
    return { type: "delete", path };
  }
  function AssertDiffable(value) {
    if (globalThis.Object.getOwnPropertySymbols(value).length > 0)
      throw new ValueDiffError(value, "Cannot diff objects with symbols");
  }
  function* ObjectType4(path, current, next) {
    AssertDiffable(current);
    AssertDiffable(next);
    if (!IsStandardObject(next))
      return yield CreateUpdate(path, next);
    const currentKeys = globalThis.Object.getOwnPropertyNames(current);
    const nextKeys = globalThis.Object.getOwnPropertyNames(next);
    for (const key of nextKeys) {
      if (HasPropertyKey2(current, key))
        continue;
      yield CreateInsert(`${path}/${key}`, next[key]);
    }
    for (const key of currentKeys) {
      if (!HasPropertyKey2(next, key))
        continue;
      if (Equal(current, next))
        continue;
      yield* Visit15(`${path}/${key}`, current[key], next[key]);
    }
    for (const key of currentKeys) {
      if (HasPropertyKey2(next, key))
        continue;
      yield CreateDelete(`${path}/${key}`);
    }
  }
  function* ArrayType4(path, current, next) {
    if (!IsArray2(next))
      return yield CreateUpdate(path, next);
    for (let i = 0; i < Math.min(current.length, next.length); i++) {
      yield* Visit15(`${path}/${i}`, current[i], next[i]);
    }
    for (let i = 0; i < next.length; i++) {
      if (i < current.length)
        continue;
      yield CreateInsert(`${path}/${i}`, next[i]);
    }
    for (let i = current.length - 1; i >= 0; i--) {
      if (i < next.length)
        continue;
      yield CreateDelete(`${path}/${i}`);
    }
  }
  function* TypedArrayType2(path, current, next) {
    if (!IsTypedArray(next) || current.length !== next.length || globalThis.Object.getPrototypeOf(current).constructor.name !== globalThis.Object.getPrototypeOf(next).constructor.name)
      return yield CreateUpdate(path, next);
    for (let i = 0; i < Math.min(current.length, next.length); i++) {
      yield* Visit15(`${path}/${i}`, current[i], next[i]);
    }
  }
  function* ValueType2(path, current, next) {
    if (current === next)
      return;
    yield CreateUpdate(path, next);
  }
  function* Visit15(path, current, next) {
    if (IsStandardObject(current))
      return yield* ObjectType4(path, current, next);
    if (IsArray2(current))
      return yield* ArrayType4(path, current, next);
    if (IsTypedArray(current))
      return yield* TypedArrayType2(path, current, next);
    if (IsValueType(current))
      return yield* ValueType2(path, current, next);
    throw new ValueDiffError(current, "Unable to diff value");
  }
  function Diff(current, next) {
    return [...Visit15("", current, next)];
  }
  function IsRootUpdate(edits) {
    return edits.length > 0 && edits[0].path === "" && edits[0].type === "update";
  }
  function IsIdentity(edits) {
    return edits.length === 0;
  }
  function Patch(current, edits) {
    if (IsRootUpdate(edits)) {
      return Clone2(edits[0].value);
    }
    if (IsIdentity(edits)) {
      return Clone2(current);
    }
    const clone = Clone2(current);
    for (const edit of edits) {
      switch (edit.type) {
        case "insert": {
          pointer_exports.Set(clone, edit.path, edit.value);
          break;
        }
        case "update": {
          pointer_exports.Set(clone, edit.path, edit.value);
          break;
        }
        case "delete": {
          pointer_exports.Delete(clone, edit.path);
          break;
        }
      }
    }
    return clone;
  }

  // ../../node_modules/@sinclair/typebox/build/esm/value/encode/encode.mjs
  function Encode(...args) {
    const [schema, references, value] = args.length === 3 ? [args[0], args[1], args[2]] : [args[0], [], args[1]];
    const encoded = HasTransform(schema, references) ? TransformEncode(schema, references, value) : value;
    if (!Check(schema, references, encoded))
      throw new TransformEncodeCheckError(schema, encoded, Errors(schema, references, encoded).First());
    return encoded;
  }

  // ../../node_modules/@sinclair/typebox/build/esm/value/mutate/mutate.mjs
  function IsStandardObject2(value) {
    return IsObject2(value) && !IsArray2(value);
  }
  var ValueMutateError = class extends TypeBoxError {
    constructor(message) {
      super(message);
    }
  };
  function ObjectType5(root, path, current, next) {
    if (!IsStandardObject2(current)) {
      pointer_exports.Set(root, path, Clone2(next));
    } else {
      const currentKeys = Object.getOwnPropertyNames(current);
      const nextKeys = Object.getOwnPropertyNames(next);
      for (const currentKey of currentKeys) {
        if (!nextKeys.includes(currentKey)) {
          delete current[currentKey];
        }
      }
      for (const nextKey of nextKeys) {
        if (!currentKeys.includes(nextKey)) {
          current[nextKey] = null;
        }
      }
      for (const nextKey of nextKeys) {
        Visit16(root, `${path}/${nextKey}`, current[nextKey], next[nextKey]);
      }
    }
  }
  function ArrayType5(root, path, current, next) {
    if (!IsArray2(current)) {
      pointer_exports.Set(root, path, Clone2(next));
    } else {
      for (let index = 0; index < next.length; index++) {
        Visit16(root, `${path}/${index}`, current[index], next[index]);
      }
      current.splice(next.length);
    }
  }
  function TypedArrayType3(root, path, current, next) {
    if (IsTypedArray(current) && current.length === next.length) {
      for (let i = 0; i < current.length; i++) {
        current[i] = next[i];
      }
    } else {
      pointer_exports.Set(root, path, Clone2(next));
    }
  }
  function ValueType3(root, path, current, next) {
    if (current === next)
      return;
    pointer_exports.Set(root, path, next);
  }
  function Visit16(root, path, current, next) {
    if (IsArray2(next))
      return ArrayType5(root, path, current, next);
    if (IsTypedArray(next))
      return TypedArrayType3(root, path, current, next);
    if (IsStandardObject2(next))
      return ObjectType5(root, path, current, next);
    if (IsValueType(next))
      return ValueType3(root, path, current, next);
  }
  function IsNonMutableValue(value) {
    return IsTypedArray(value) || IsValueType(value);
  }
  function IsMismatchedValue(current, next) {
    return IsStandardObject2(current) && IsArray2(next) || IsArray2(current) && IsStandardObject2(next);
  }
  function Mutate(current, next) {
    if (IsNonMutableValue(current) || IsNonMutableValue(next))
      throw new ValueMutateError("Only object and array types can be mutated at the root level");
    if (IsMismatchedValue(current, next))
      throw new ValueMutateError("Cannot assign due type mismatch of assignable values");
    Visit16(current, "", current, next);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/value/parse/parse.mjs
  var ParseError = class extends TypeBoxError {
    constructor(message) {
      super(message);
    }
  };
  var ParseRegistry;
  (function(ParseRegistry2) {
    const registry = /* @__PURE__ */ new Map([
      ["Assert", (type, references, value) => {
        Assert(type, references, value);
        return value;
      }],
      ["Cast", (type, references, value) => Cast(type, references, value)],
      ["Clean", (type, references, value) => Clean(type, references, value)],
      ["Clone", (_type, _references, value) => Clone2(value)],
      ["Convert", (type, references, value) => Convert(type, references, value)],
      ["Decode", (type, references, value) => HasTransform(type, references) ? TransformDecode(type, references, value) : value],
      ["Default", (type, references, value) => Default5(type, references, value)],
      ["Encode", (type, references, value) => HasTransform(type, references) ? TransformEncode(type, references, value) : value]
    ]);
    function Delete5(key) {
      registry.delete(key);
    }
    ParseRegistry2.Delete = Delete5;
    function Set5(key, callback) {
      registry.set(key, callback);
    }
    ParseRegistry2.Set = Set5;
    function Get4(key) {
      return registry.get(key);
    }
    ParseRegistry2.Get = Get4;
  })(ParseRegistry || (ParseRegistry = {}));
  var ParseDefault = [
    "Clone",
    "Clean",
    "Default",
    "Convert",
    "Assert",
    "Decode"
  ];
  function ParseValue(operations, type, references, value) {
    return operations.reduce((value2, operationKey) => {
      const operation = ParseRegistry.Get(operationKey);
      if (IsUndefined2(operation))
        throw new ParseError(`Unable to find Parse operation '${operationKey}'`);
      return operation(type, references, value2);
    }, value);
  }
  function Parse(...args) {
    const [operations, schema, references, value] = args.length === 4 ? [args[0], args[1], args[2], args[3]] : args.length === 3 ? IsArray2(args[0]) ? [args[0], args[1], [], args[2]] : [ParseDefault, args[0], args[1], args[2]] : args.length === 2 ? [ParseDefault, args[0], [], args[1]] : (() => {
      throw new ParseError("Invalid Arguments");
    })();
    return ParseValue(operations, schema, references, value);
  }

  // ../../node_modules/@sinclair/typebox/build/esm/value/value/value.mjs
  var value_exports2 = {};
  __export(value_exports2, {
    Assert: () => Assert,
    Cast: () => Cast,
    Check: () => Check,
    Clean: () => Clean,
    Clone: () => Clone2,
    Convert: () => Convert,
    Create: () => Create2,
    Decode: () => Decode,
    Default: () => Default5,
    Diff: () => Diff,
    Edit: () => Edit,
    Encode: () => Encode,
    Equal: () => Equal,
    Errors: () => Errors,
    Hash: () => Hash,
    Mutate: () => Mutate,
    Parse: () => Parse,
    Patch: () => Patch,
    ValueErrorIterator: () => ValueErrorIterator
  });

  // ../../packages/ai/dist/utils/validation.js
  function validateToolArguments(tool, toolCall) {
    const args = toolCall.arguments ?? {};
    if (value_exports2.Check(tool.parameters, args)) {
      return args;
    }
    const firstError = Array.from(value_exports2.Errors(tool.parameters, args))[0];
    if (!firstError) {
      throw new Error(`Invalid arguments for tool "${tool.name}"`);
    }
    const path = firstError.path || "/";
    throw new Error(`Invalid arguments for tool "${tool.name}" at ${path}: ${firstError.message}`);
  }

  // ../../packages/ai/dist/index.js
  registerApiProvider({
    api: "openai-completions",
    stream: streamOpenAICompletions,
    streamSimple: streamSimpleOpenAICompletions
  });
  registerApiProvider({
    api: "anthropic-messages",
    stream: streamAnthropic,
    streamSimple: streamSimpleAnthropic
  });

  // ../../packages/agent-core/dist/agent-loop.js
  function agentLoop(prompts, context, config, signal, streamFn) {
    const stream = createAgentStream();
    (async () => {
      try {
        await config.hooks?.beforeAgentStart?.();
      } catch (e) {
        console.warn("[AgentLoop] beforeAgentStart hook failed:", e);
      }
      const newMessages = [...prompts];
      const currentContext = {
        ...context,
        messages: [...context.messages, ...prompts]
      };
      stream.push({ type: "agent_start" });
      stream.push({ type: "turn_start" });
      for (const prompt of prompts) {
        stream.push({ type: "message_start", message: prompt });
        stream.push({ type: "message_end", message: prompt });
      }
      try {
        await runLoop(currentContext, newMessages, config, signal, stream, streamFn);
      } catch (error) {
        config.hooks?.onError?.(error, { phase: "runLoop" });
        throw error;
      }
      try {
        await config.hooks?.afterAgentEnd?.(newMessages);
      } catch (e) {
        console.warn("[AgentLoop] afterAgentEnd hook failed:", e);
      }
    })();
    return stream;
  }
  function agentLoopContinue(context, config, signal, streamFn) {
    if (context.messages.length === 0) {
      throw new Error("Cannot continue: no messages in context");
    }
    if (context.messages[context.messages.length - 1].role === "assistant") {
      throw new Error("Cannot continue from message role: assistant");
    }
    const stream = createAgentStream();
    (async () => {
      try {
        await config.hooks?.beforeAgentStart?.();
      } catch (e) {
        console.warn("[AgentLoop] beforeAgentStart hook failed:", e);
      }
      const newMessages = [];
      const currentContext = { ...context };
      stream.push({ type: "agent_start" });
      stream.push({ type: "turn_start" });
      try {
        await runLoop(currentContext, newMessages, config, signal, stream, streamFn);
      } catch (error) {
        config.hooks?.onError?.(error, { phase: "runLoop" });
        throw error;
      }
      try {
        await config.hooks?.afterAgentEnd?.(newMessages);
      } catch (e) {
        console.warn("[AgentLoop] afterAgentEnd hook failed:", e);
      }
    })();
    return stream;
  }
  function createAgentStream() {
    return new EventStream((event) => event.type === "agent_end", (event) => event.type === "agent_end" ? event.messages : []);
  }
  async function runLoop(currentContext, newMessages, config, signal, stream, streamFn) {
    let firstTurn = true;
    let pendingMessages = await config.getSteeringMessages?.() || [];
    while (true) {
      let hasMoreToolCalls = true;
      let steeringAfterTools = null;
      while (hasMoreToolCalls || pendingMessages.length > 0) {
        if (!firstTurn) {
          stream.push({ type: "turn_start" });
        } else {
          firstTurn = false;
        }
        if (pendingMessages.length > 0) {
          for (const message2 of pendingMessages) {
            stream.push({ type: "message_start", message: message2 });
            stream.push({ type: "message_end", message: message2 });
            currentContext.messages.push(message2);
            newMessages.push(message2);
          }
          pendingMessages = [];
        }
        let message;
        try {
          message = await streamAssistantResponse(currentContext, config, signal, stream, streamFn);
        } catch (error) {
          config.hooks?.onError?.(error, { phase: "streamAssistantResponse" });
          throw error;
        }
        newMessages.push(message);
        if (message.stopReason === "error" || message.stopReason === "aborted") {
          stream.push({ type: "turn_end", message, toolResults: [] });
          stream.push({ type: "agent_end", messages: newMessages });
          stream.end(newMessages);
          return;
        }
        const toolCalls = message.content.filter((c) => c.type === "toolCall");
        hasMoreToolCalls = toolCalls.length > 0;
        const toolResults = [];
        if (hasMoreToolCalls) {
          const toolExecution = await executeToolCalls(currentContext.tools, message, signal, stream, config.getSteeringMessages, config.hooks);
          toolResults.push(...toolExecution.toolResults);
          steeringAfterTools = toolExecution.steeringMessages ?? null;
          for (const result of toolResults) {
            currentContext.messages.push(result);
            newMessages.push(result);
          }
        }
        stream.push({ type: "turn_end", message, toolResults });
        if (steeringAfterTools && steeringAfterTools.length > 0) {
          pendingMessages = steeringAfterTools;
          steeringAfterTools = null;
        } else {
          pendingMessages = await config.getSteeringMessages?.() || [];
        }
      }
      const followUpMessages = await config.getFollowUpMessages?.() || [];
      if (followUpMessages.length > 0) {
        pendingMessages = followUpMessages;
        continue;
      }
      break;
    }
    stream.push({ type: "agent_end", messages: newMessages });
    stream.end(newMessages);
  }
  async function streamAssistantResponse(context, config, signal, stream, streamFn) {
    let messages = context.messages;
    if (config.transformContext) {
      messages = await config.transformContext(messages, signal);
    }
    const llmMessages = await config.convertToLlm(messages);
    const llmContext = {
      systemPrompt: context.systemPrompt,
      messages: llmMessages,
      tools: context.tools
    };
    const streamFunction = streamFn || streamSimple;
    const resolvedApiKey = (config.getApiKey ? await config.getApiKey(config.model.provider) : void 0) || config.apiKey;
    const response = await streamFunction(config.model, llmContext, {
      ...config,
      apiKey: resolvedApiKey,
      signal
    });
    let partialMessage = null;
    let addedPartial = false;
    for await (const event of response) {
      switch (event.type) {
        case "start":
          partialMessage = event.partial;
          context.messages.push(partialMessage);
          addedPartial = true;
          stream.push({ type: "message_start", message: { ...partialMessage } });
          break;
        case "text_start":
        case "text_delta":
        case "text_end":
        case "thinking_start":
        case "thinking_delta":
        case "thinking_end":
        case "toolcall_start":
        case "toolcall_delta":
        case "toolcall_end":
          if (partialMessage) {
            partialMessage = event.partial;
            context.messages[context.messages.length - 1] = partialMessage;
            stream.push({
              type: "message_update",
              assistantMessageEvent: event,
              message: { ...partialMessage }
            });
          }
          break;
        case "done":
        case "error": {
          const finalMessage = await response.result();
          if (addedPartial) {
            context.messages[context.messages.length - 1] = finalMessage;
          } else {
            context.messages.push(finalMessage);
          }
          if (!addedPartial) {
            stream.push({ type: "message_start", message: { ...finalMessage } });
          }
          stream.push({ type: "message_end", message: finalMessage });
          return finalMessage;
        }
      }
    }
    return await response.result();
  }
  async function executeToolCalls(tools, assistantMessage, signal, stream, getSteeringMessages, hooks) {
    const toolCalls = assistantMessage.content.filter((c) => c.type === "toolCall");
    const results = [];
    let steeringMessages;
    for (let index = 0; index < toolCalls.length; index++) {
      const toolCall = toolCalls[index];
      const tool = tools?.find((t) => t.name === toolCall.name);
      let blocked = false;
      let blockReason;
      try {
        const hookResult = await hooks?.beforeToolCall?.(toolCall.name, toolCall.arguments);
        if (hookResult && hookResult.block) {
          blocked = true;
          blockReason = hookResult.reason || "Blocked by hook";
        }
      } catch (e) {
        console.warn(`[AgentLoop] beforeToolCall hook failed for ${toolCall.name}:`, e);
      }
      stream.push({
        type: "tool_execution_start",
        toolCallId: toolCall.id,
        toolName: toolCall.name,
        args: toolCall.arguments
      });
      let result;
      let isError = false;
      if (blocked) {
        result = {
          content: [{ type: "text", text: `Tool execution blocked: ${blockReason}` }],
          details: { blocked: true, reason: blockReason }
        };
        isError = true;
      } else {
        try {
          if (!tool) {
            const availableTools = tools?.map((t) => t.name).join(", ") || "none";
            const blockMessage = `I don't have permission to use the "${toolCall.name}" tool. My available tools are: [${availableTools}]. I can only perform read-only operations. Would you like me to help you with something else?`;
            console.warn(`[AgentLoop] BLOCKED: Tool "${toolCall.name}" not in whitelist. Available: [${availableTools}]`);
            result = {
              content: [{ type: "text", text: blockMessage }],
              details: { blockedByWhitelist: true, requestedTool: toolCall.name, availableTools }
            };
            isError = false;
          } else {
            const validatedArgs = validateToolArguments(tool, toolCall);
            result = await tool.execute(toolCall.id, validatedArgs, signal, (partialResult) => {
              stream.push({
                type: "tool_execution_update",
                toolCallId: toolCall.id,
                toolName: toolCall.name,
                args: toolCall.arguments,
                partialResult
              });
            });
          }
        } catch (e) {
          result = {
            content: [{ type: "text", text: e instanceof Error ? e.message : String(e) }],
            details: {}
          };
          isError = true;
          hooks?.onError?.(e instanceof Error ? e : new Error(String(e)), {
            toolName: toolCall.name,
            phase: "toolExecution"
          });
        }
      }
      try {
        await hooks?.afterToolCall?.(toolCall.name, toolCall.arguments, result, isError);
      } catch (e) {
        console.warn(`[AgentLoop] afterToolCall hook failed for ${toolCall.name}:`, e);
      }
      stream.push({
        type: "tool_execution_end",
        toolCallId: toolCall.id,
        toolName: toolCall.name,
        result,
        isError
      });
      const toolResultMessage = {
        role: "toolResult",
        toolCallId: toolCall.id,
        toolName: toolCall.name,
        content: result.content,
        details: result.details,
        isError,
        timestamp: Date.now()
      };
      results.push(toolResultMessage);
      stream.push({ type: "message_start", message: toolResultMessage });
      stream.push({ type: "message_end", message: toolResultMessage });
      if (getSteeringMessages) {
        const steering = await getSteeringMessages();
        if (steering.length > 0) {
          steeringMessages = steering;
          const remainingCalls = toolCalls.slice(index + 1);
          for (const skipped of remainingCalls) {
            results.push(skipToolCall(skipped, stream));
          }
          break;
        }
      }
    }
    return { toolResults: results, steeringMessages };
  }
  function skipToolCall(toolCall, stream) {
    const result = {
      content: [{ type: "text", text: "Skipped due to queued user message." }],
      details: {}
    };
    stream.push({
      type: "tool_execution_start",
      toolCallId: toolCall.id,
      toolName: toolCall.name,
      args: toolCall.arguments
    });
    stream.push({
      type: "tool_execution_end",
      toolCallId: toolCall.id,
      toolName: toolCall.name,
      result,
      isError: true
    });
    const toolResultMessage = {
      role: "toolResult",
      toolCallId: toolCall.id,
      toolName: toolCall.name,
      content: result.content,
      details: {},
      isError: true,
      timestamp: Date.now()
    };
    stream.push({ type: "message_start", message: toolResultMessage });
    stream.push({ type: "message_end", message: toolResultMessage });
    return toolResultMessage;
  }

  // ../../packages/agent-core/dist/agent.js
  function defaultConvertToLlm(messages) {
    return messages.filter((m) => m.role === "user" || m.role === "assistant" || m.role === "toolResult");
  }
  function isAgentMessage(value) {
    return !!value && typeof value === "object" && "role" in value;
  }
  function isUserContentBlock(value) {
    if (!value || typeof value !== "object" || !("type" in value)) {
      return false;
    }
    const type = value.type;
    return type === "text" || type === "image" || type === "audio" || type === "video" || type === "fileRef";
  }
  var Agent = class {
    _state;
    listeners = /* @__PURE__ */ new Set();
    abortController;
    convertToLlm;
    transformContext;
    steeringQueue = [];
    followUpQueue = [];
    steeringMode;
    followUpMode;
    streamFn;
    _sessionId;
    _apiKey;
    getApiKey;
    runningPrompt;
    resolveRunningPrompt;
    _thinkingBudgets;
    _maxRetryDelayMs;
    constructor(opts = {}) {
      this._state = {
        systemPrompt: "",
        model: null,
        // Must be set before use
        thinkingLevel: "off",
        tools: [],
        messages: [],
        isStreaming: false,
        streamMessage: null,
        pendingToolCalls: /* @__PURE__ */ new Set(),
        error: void 0,
        ...opts.initialState
      };
      this.convertToLlm = opts.convertToLlm || defaultConvertToLlm;
      this.transformContext = opts.transformContext;
      this.steeringMode = opts.steeringMode || "one-at-a-time";
      this.followUpMode = opts.followUpMode || "one-at-a-time";
      this.streamFn = opts.streamFn || streamSimple;
      this._sessionId = opts.sessionId;
      this._apiKey = opts.apiKey;
      this.getApiKey = opts.getApiKey;
      this._thinkingBudgets = opts.thinkingBudgets;
      this._maxRetryDelayMs = opts.maxRetryDelayMs;
    }
    get sessionId() {
      return this._sessionId;
    }
    set sessionId(value) {
      this._sessionId = value;
    }
    get apiKey() {
      return this._apiKey;
    }
    set apiKey(value) {
      this._apiKey = value;
    }
    get thinkingBudgets() {
      return this._thinkingBudgets;
    }
    set thinkingBudgets(value) {
      this._thinkingBudgets = value;
    }
    get maxRetryDelayMs() {
      return this._maxRetryDelayMs;
    }
    set maxRetryDelayMs(value) {
      this._maxRetryDelayMs = value;
    }
    get state() {
      return this._state;
    }
    subscribe(fn) {
      this.listeners.add(fn);
      return () => this.listeners.delete(fn);
    }
    setSystemPrompt(v) {
      this._state.systemPrompt = v;
    }
    setModel(m) {
      this._state.model = m;
    }
    setThinkingLevel(l) {
      this._state.thinkingLevel = l;
    }
    setTools(t) {
      this._state.tools = t;
    }
    replaceMessages(ms) {
      this._state.messages = ms.slice();
    }
    appendMessage(m) {
      this._state.messages = [...this._state.messages, m];
    }
    clearMessages() {
      this._state.messages = [];
    }
    steer(m) {
      this.steeringQueue.push(m);
    }
    followUp(m) {
      this.followUpQueue.push(m);
    }
    clearSteeringQueue() {
      this.steeringQueue = [];
    }
    clearFollowUpQueue() {
      this.followUpQueue = [];
    }
    clearAllQueues() {
      this.steeringQueue = [];
      this.followUpQueue = [];
    }
    hasQueuedMessages() {
      return this.steeringQueue.length > 0 || this.followUpQueue.length > 0;
    }
    setSteeringMode(mode) {
      this.steeringMode = mode;
    }
    getSteeringMode() {
      return this.steeringMode;
    }
    setFollowUpMode(mode) {
      this.followUpMode = mode;
    }
    getFollowUpMode() {
      return this.followUpMode;
    }
    dequeueSteeringMessages() {
      if (this.steeringMode === "one-at-a-time") {
        if (this.steeringQueue.length > 0) {
          const first = this.steeringQueue[0];
          this.steeringQueue = this.steeringQueue.slice(1);
          return [first];
        }
        return [];
      }
      const steering = this.steeringQueue.slice();
      this.steeringQueue = [];
      return steering;
    }
    dequeueFollowUpMessages() {
      if (this.followUpMode === "one-at-a-time") {
        if (this.followUpQueue.length > 0) {
          const first = this.followUpQueue[0];
          this.followUpQueue = this.followUpQueue.slice(1);
          return [first];
        }
        return [];
      }
      const followUp = this.followUpQueue.slice();
      this.followUpQueue = [];
      return followUp;
    }
    abort() {
      this.abortController?.abort();
    }
    waitForIdle() {
      return this.runningPrompt ?? Promise.resolve();
    }
    reset() {
      this._state.messages = [];
      this._state.isStreaming = false;
      this._state.streamMessage = null;
      this._state.pendingToolCalls = /* @__PURE__ */ new Set();
      this._state.error = void 0;
      this.steeringQueue = [];
      this.followUpQueue = [];
      this.runningPrompt = void 0;
      this.resolveRunningPrompt = void 0;
    }
    /**
     * Reset only the processing state without clearing messages.
     * Useful when resuming a session that was saved mid-processing.
     */
    resetProcessingState() {
      this._state.isStreaming = false;
      this._state.streamMessage = null;
      this._state.pendingToolCalls = /* @__PURE__ */ new Set();
      this._state.error = void 0;
      this.steeringQueue = [];
      this.followUpQueue = [];
      this.runningPrompt = void 0;
      this.resolveRunningPrompt = void 0;
      this.abortController = void 0;
    }
    async prompt(input, images) {
      if (this.runningPrompt) {
        throw new Error("Agent is already processing a prompt.");
      }
      if (this._state.isStreaming) {
        throw new Error("Agent is already processing a prompt.");
      }
      const promise = new Promise((resolve) => {
        this.resolveRunningPrompt = resolve;
      });
      this.runningPrompt = promise;
      try {
        const model = this._state.model;
        if (!model)
          throw new Error("No model configured");
        let msgs;
        if (Array.isArray(input)) {
          if (input.length > 0 && input.every((item) => isUserContentBlock(item))) {
            msgs = [{ role: "user", content: input, timestamp: Date.now() }];
          } else if (input.every((item) => isAgentMessage(item))) {
            msgs = input;
          } else {
            throw new Error("Invalid prompt array: expected user content blocks or agent messages.");
          }
        } else if (typeof input === "string") {
          const content = [{ type: "text", text: input }];
          if (images && images.length > 0) {
            content.push(...images);
          }
          msgs = [{ role: "user", content, timestamp: Date.now() }];
        } else {
          msgs = [input];
        }
        await this._runLoop(msgs);
      } finally {
        if (this.runningPrompt === promise) {
          this.resolveRunningPrompt?.();
          this.runningPrompt = void 0;
          this.resolveRunningPrompt = void 0;
        }
      }
    }
    async continue() {
      if (this.runningPrompt) {
        throw new Error("Agent is already processing.");
      }
      if (this._state.isStreaming) {
        throw new Error("Agent is already processing.");
      }
      const promise = new Promise((resolve) => {
        this.resolveRunningPrompt = resolve;
      });
      this.runningPrompt = promise;
      try {
        const messages = this._state.messages;
        if (messages.length === 0) {
          throw new Error("No messages to continue from");
        }
        if (messages[messages.length - 1].role === "assistant") {
          const queuedSteering = this.dequeueSteeringMessages();
          if (queuedSteering.length > 0) {
            await this._runLoop(queuedSteering, { skipInitialSteeringPoll: true });
            return;
          }
          const queuedFollowUp = this.dequeueFollowUpMessages();
          if (queuedFollowUp.length > 0) {
            await this._runLoop(queuedFollowUp);
            return;
          }
          throw new Error("Cannot continue from message role: assistant");
        }
        await this._runLoop(void 0);
      } finally {
        if (this.runningPrompt === promise) {
          this.resolveRunningPrompt?.();
          this.runningPrompt = void 0;
          this.resolveRunningPrompt = void 0;
        }
      }
    }
    async _runLoop(messages, options) {
      const model = this._state.model;
      if (!model)
        throw new Error("No model configured");
      this.abortController = new AbortController();
      this._state.isStreaming = true;
      this._state.streamMessage = null;
      this._state.error = void 0;
      const reasoning = this._state.thinkingLevel === "off" ? void 0 : this._state.thinkingLevel;
      const context = {
        systemPrompt: this._state.systemPrompt,
        messages: this._state.messages.slice(),
        tools: this._state.tools
      };
      let skipInitialSteeringPoll = options?.skipInitialSteeringPoll === true;
      const config = {
        model,
        reasoning,
        apiKey: this._apiKey,
        sessionId: this._sessionId,
        thinkingBudgets: this._thinkingBudgets,
        maxRetryDelayMs: this._maxRetryDelayMs,
        convertToLlm: this.convertToLlm,
        transformContext: this.transformContext,
        getApiKey: this.getApiKey,
        getSteeringMessages: async () => {
          if (skipInitialSteeringPoll) {
            skipInitialSteeringPoll = false;
            return [];
          }
          return this.dequeueSteeringMessages();
        },
        getFollowUpMessages: async () => this.dequeueFollowUpMessages()
      };
      let partial = null;
      try {
        const stream = messages ? agentLoop(messages, context, config, this.abortController.signal, this.streamFn) : agentLoopContinue(context, config, this.abortController.signal, this.streamFn);
        for await (const event of stream) {
          switch (event.type) {
            case "message_start":
              partial = event.message;
              this._state.streamMessage = event.message;
              break;
            case "message_update":
              partial = event.message;
              this._state.streamMessage = event.message;
              break;
            case "message_end":
              partial = null;
              this._state.streamMessage = null;
              this.appendMessage(event.message);
              break;
            case "tool_execution_start": {
              const s = new Set(this._state.pendingToolCalls);
              s.add(event.toolCallId);
              this._state.pendingToolCalls = s;
              break;
            }
            case "tool_execution_end": {
              const s = new Set(this._state.pendingToolCalls);
              s.delete(event.toolCallId);
              this._state.pendingToolCalls = s;
              break;
            }
            case "turn_end":
              if (event.message.role === "assistant" && event.message.errorMessage) {
                this._state.error = event.message.errorMessage;
              }
              break;
            case "agent_end":
              this._state.isStreaming = false;
              this._state.streamMessage = null;
              break;
          }
          this.emit(event);
        }
        if (partial && partial.role === "assistant" && partial.content?.length > 0) {
          const onlyEmpty = !partial.content.some((c) => c.type === "thinking" && c.thinking?.trim().length > 0 || c.type === "text" && c.text?.trim().length > 0 || c.type === "toolCall" && c.name?.trim().length > 0);
          if (!onlyEmpty) {
            this.appendMessage(partial);
          }
        }
      } catch (err) {
        const errorMsg = {
          role: "assistant",
          content: [{ type: "text", text: "" }],
          api: model.api,
          provider: model.provider,
          model: model.id,
          usage: {
            input: 0,
            output: 0,
            cacheRead: 0,
            cacheWrite: 0,
            totalTokens: 0,
            cost: { input: 0, output: 0, cacheRead: 0, cacheWrite: 0, total: 0 }
          },
          stopReason: this.abortController?.signal.aborted ? "aborted" : "error",
          errorMessage: err?.message || String(err),
          timestamp: Date.now()
        };
        this.appendMessage(errorMsg);
        this._state.error = err?.message || String(err);
        this.emit({ type: "agent_end", messages: [errorMsg] });
      } finally {
        this._state.isStreaming = false;
        this._state.streamMessage = null;
        this._state.pendingToolCalls = /* @__PURE__ */ new Set();
        this.abortController = void 0;
      }
    }
    emit(e) {
      for (const listener of this.listeners) {
        listener(e);
      }
    }
  };

  // ../../packages/tools-fs/dist/path-utils.js
  function isInAllowlist(path, allowlistedDirs) {
    const normalized = normalizeForComparison(path);
    for (const dir of allowlistedDirs) {
      const normalizedDir = normalizeForComparison(dir);
      const dirWithSlash = withTrailingSep(normalizedDir);
      if (normalized === normalizedDir || normalized.startsWith(dirWithSlash)) {
        return true;
      }
    }
    return false;
  }
  function normalizeForComparison(path) {
    const normalized = path.replace(/\\/g, "/").replace(/\/+/g, "/");
    if (normalized === "/")
      return normalized;
    return normalized.replace(/\/+$/, "");
  }
  function withTrailingSep(path) {
    return path.endsWith("/") ? path : `${path}/`;
  }
  function isWithinRoot(path, root) {
    const normalizedPath = normalizeForComparison(path);
    const normalizedRoot = normalizeForComparison(root);
    return normalizedPath === normalizedRoot || normalizedPath.startsWith(withTrailingSep(normalizedRoot));
  }
  function resolveInSandbox(filePath, sandboxRoot, options) {
    const fs = getFileSystem();
    const allowlistedDirs = options?.allowlistedDirs ?? [];
    if (!filePath) {
      throw new Error("Path cannot be empty");
    }
    const cleaned = filePath.startsWith("@") ? filePath.slice(1) : filePath;
    if (fs.isAbsolute(cleaned)) {
      const normalized = fs.normalize(cleaned);
      if (isInAllowlist(normalized, allowlistedDirs)) {
        return normalized;
      }
      if (!isWithinRoot(normalized, sandboxRoot)) {
        throw new Error(`Absolute path outside sandbox: ${filePath}`);
      }
      return normalized;
    }
    const resolved = fs.resolve(sandboxRoot, cleaned);
    if (!isWithinRoot(resolved, sandboxRoot) && !isInAllowlist(resolved, allowlistedDirs)) {
      throw new Error(`Path escapes sandbox: ${filePath}`);
    }
    return resolved;
  }
  var resolveToCwd = resolveInSandbox;
  var resolveReadPath = resolveInSandbox;

  // ../../packages/tools-fs/dist/truncate.js
  var DEFAULT_MAX_LINES = 2e3;
  var DEFAULT_MAX_BYTES = 50 * 1024;
  var GREP_MAX_LINE_LENGTH = 500;
  function formatSize(bytes) {
    if (bytes < 1024) {
      return `${bytes}B`;
    } else if (bytes < 1024 * 1024) {
      return `${(bytes / 1024).toFixed(1)}KB`;
    } else {
      return `${(bytes / (1024 * 1024)).toFixed(1)}MB`;
    }
  }
  function truncateHead(content, options = {}) {
    const maxLines = options.maxLines ?? DEFAULT_MAX_LINES;
    const maxBytes = options.maxBytes ?? DEFAULT_MAX_BYTES;
    const totalBytes = Buffer.byteLength(content, "utf-8");
    const lines = content.split("\n");
    const totalLines = lines.length;
    if (totalLines <= maxLines && totalBytes <= maxBytes) {
      return {
        content,
        truncated: false,
        truncatedBy: null,
        totalLines,
        totalBytes,
        outputLines: totalLines,
        outputBytes: totalBytes,
        lastLinePartial: false,
        firstLineExceedsLimit: false,
        maxLines,
        maxBytes
      };
    }
    const firstLineBytes = Buffer.byteLength(lines[0], "utf-8");
    if (firstLineBytes > maxBytes) {
      return {
        content: "",
        truncated: true,
        truncatedBy: "bytes",
        totalLines,
        totalBytes,
        outputLines: 0,
        outputBytes: 0,
        lastLinePartial: false,
        firstLineExceedsLimit: true,
        maxLines,
        maxBytes
      };
    }
    const outputLinesArr = [];
    let outputBytesCount = 0;
    let truncatedBy = "lines";
    for (let i = 0; i < lines.length && i < maxLines; i++) {
      const line = lines[i];
      const lineBytes = Buffer.byteLength(line, "utf-8") + (i > 0 ? 1 : 0);
      if (outputBytesCount + lineBytes > maxBytes) {
        truncatedBy = "bytes";
        break;
      }
      outputLinesArr.push(line);
      outputBytesCount += lineBytes;
    }
    if (outputLinesArr.length >= maxLines && outputBytesCount <= maxBytes) {
      truncatedBy = "lines";
    }
    const outputContent = outputLinesArr.join("\n");
    const finalOutputBytes = Buffer.byteLength(outputContent, "utf-8");
    return {
      content: outputContent,
      truncated: true,
      truncatedBy,
      totalLines,
      totalBytes,
      outputLines: outputLinesArr.length,
      outputBytes: finalOutputBytes,
      lastLinePartial: false,
      firstLineExceedsLimit: false,
      maxLines,
      maxBytes
    };
  }

  // ../../packages/tools-fs/dist/read.js
  var readSchema = Type.Object({
    path: Type.String({ description: "Path to the file to read (relative to sandbox root)" }),
    offset: Type.Optional(Type.Number({ description: "Line number to start reading from (1-indexed)" })),
    limit: Type.Optional(Type.Number({ description: "Maximum number of lines to read" }))
  });
  function createReadTool(cwd, options) {
    const fs = getFileSystem();
    return {
      name: "read",
      label: "read",
      description: `Read the contents of a file. For text files, output is truncated to ${DEFAULT_MAX_LINES} lines or ${DEFAULT_MAX_BYTES / 1024}KB (whichever is hit first). Use offset/limit for large files. When you need the full file, continue with offset until complete.`,
      parameters: readSchema,
      execute: async (_toolCallId, params, signal) => {
        const { path, offset, limit } = params;
        const absolutePath = resolveReadPath(path, cwd, options);
        return new Promise((resolve, reject) => {
          if (signal?.aborted) {
            reject(new Error("Operation aborted"));
            return;
          }
          let aborted = false;
          const onAbort = () => {
            aborted = true;
            reject(new Error("Operation aborted"));
          };
          if (signal) {
            signal.addEventListener("abort", onAbort, { once: true });
          }
          (async () => {
            try {
              await fs.access(absolutePath);
              if (aborted)
                return;
              const content = await fs.readFile(absolutePath, "utf-8");
              const allLines = content.split("\n");
              const totalFileLines = allLines.length;
              const startLine = offset ? Math.max(0, offset - 1) : 0;
              const startLineDisplay = startLine + 1;
              if (startLine >= allLines.length) {
                throw new Error(`Offset ${offset} is beyond end of file (${allLines.length} lines total)`);
              }
              let selectedContent;
              let userLimitedLines;
              if (limit !== void 0) {
                const endLine = Math.min(startLine + limit, allLines.length);
                selectedContent = allLines.slice(startLine, endLine).join("\n");
                userLimitedLines = endLine - startLine;
              } else {
                selectedContent = allLines.slice(startLine).join("\n");
              }
              const truncation = truncateHead(selectedContent);
              let outputText;
              let details;
              if (truncation.firstLineExceedsLimit) {
                const firstLineSize = formatSize(Buffer.byteLength(allLines[startLine], "utf-8"));
                outputText = `[Line ${startLineDisplay} is ${firstLineSize}, exceeds ${formatSize(DEFAULT_MAX_BYTES)} limit.]`;
                details = { truncation };
              } else if (truncation.truncated) {
                const endLineDisplay = startLineDisplay + truncation.outputLines - 1;
                const nextOffset = endLineDisplay + 1;
                outputText = truncation.content;
                if (truncation.truncatedBy === "lines") {
                  outputText += `

[Showing lines ${startLineDisplay}-${endLineDisplay} of ${totalFileLines}. Use offset=${nextOffset} to continue.]`;
                } else {
                  outputText += `

[Showing lines ${startLineDisplay}-${endLineDisplay} of ${totalFileLines} (${formatSize(DEFAULT_MAX_BYTES)} limit). Use offset=${nextOffset} to continue.]`;
                }
                details = { truncation };
              } else if (userLimitedLines !== void 0 && startLine + userLimitedLines < allLines.length) {
                const remaining = allLines.length - (startLine + userLimitedLines);
                const nextOffset = startLine + userLimitedLines + 1;
                outputText = truncation.content;
                outputText += `

[${remaining} more lines in file. Use offset=${nextOffset} to continue.]`;
              } else {
                outputText = truncation.content;
              }
              const textContent = [{ type: "text", text: outputText }];
              if (aborted)
                return;
              if (signal) {
                signal.removeEventListener("abort", onAbort);
              }
              resolve({ content: textContent, details });
            } catch (error) {
              if (signal) {
                signal.removeEventListener("abort", onAbort);
              }
              if (!aborted) {
                reject(error);
              }
            }
          })();
        });
      }
    };
  }

  // ../../packages/tools-fs/dist/write.js
  var writeSchema = Type.Object({
    path: Type.String({ description: "Path to the file to write (relative to sandbox root)" }),
    content: Type.String({ description: "Content to write to the file" })
  });
  function createWriteTool(cwd, options) {
    const fs = getFileSystem();
    return {
      name: "write",
      label: "write",
      description: "Write content to a file. Creates the file if it doesn't exist, overwrites if it does. Automatically creates parent directories.",
      parameters: writeSchema,
      execute: async (_toolCallId, params, signal) => {
        const { path: filePath, content } = params;
        const absolutePath = resolveToCwd(filePath, cwd, options);
        const dir = fs.dirname(absolutePath);
        return new Promise((resolve, reject) => {
          if (signal?.aborted) {
            reject(new Error("Operation aborted"));
            return;
          }
          let aborted = false;
          const onAbort = () => {
            aborted = true;
            reject(new Error("Operation aborted"));
          };
          if (signal) {
            signal.addEventListener("abort", onAbort, { once: true });
          }
          (async () => {
            try {
              await fs.mkdir(dir, { recursive: true });
              if (aborted)
                return;
              await fs.writeFile(absolutePath, content, "utf-8");
              if (aborted)
                return;
              if (signal) {
                signal.removeEventListener("abort", onAbort);
              }
              resolve({
                content: [{ type: "text", text: `Successfully wrote ${content.length} bytes to ${filePath}` }],
                details: void 0
              });
            } catch (error) {
              if (signal) {
                signal.removeEventListener("abort", onAbort);
              }
              if (!aborted) {
                reject(error);
              }
            }
          })();
        });
      }
    };
  }

  // ../../packages/tools-fs/dist/edit-diff.js
  function detectLineEnding(content) {
    const crlfIdx = content.indexOf("\r\n");
    const lfIdx = content.indexOf("\n");
    if (lfIdx === -1)
      return "\n";
    if (crlfIdx === -1)
      return "\n";
    return crlfIdx < lfIdx ? "\r\n" : "\n";
  }
  function normalizeToLF(text) {
    return text.replace(/\r\n/g, "\n").replace(/\r/g, "\n");
  }
  function restoreLineEndings(text, ending) {
    return ending === "\r\n" ? text.replace(/\n/g, "\r\n") : text;
  }
  function normalizeForFuzzyMatch(text) {
    return text.split("\n").map((line) => line.trimEnd()).join("\n").replace(/[\u2018\u2019\u201A\u201B]/g, "'").replace(/[\u201C\u201D\u201E\u201F]/g, '"').replace(/[\u2010\u2011\u2012\u2013\u2014\u2015\u2212]/g, "-").replace(/[\u00A0\u2002-\u200A\u202F\u205F\u3000]/g, " ");
  }
  function fuzzyFindText(content, oldText) {
    const exactIndex = content.indexOf(oldText);
    if (exactIndex !== -1) {
      return {
        found: true,
        index: exactIndex,
        matchLength: oldText.length,
        usedFuzzyMatch: false,
        contentForReplacement: content
      };
    }
    const fuzzyContent = normalizeForFuzzyMatch(content);
    const fuzzyOldText = normalizeForFuzzyMatch(oldText);
    const fuzzyIndex = fuzzyContent.indexOf(fuzzyOldText);
    if (fuzzyIndex === -1) {
      return {
        found: false,
        index: -1,
        matchLength: 0,
        usedFuzzyMatch: false,
        contentForReplacement: content
      };
    }
    return {
      found: true,
      index: fuzzyIndex,
      matchLength: fuzzyOldText.length,
      usedFuzzyMatch: true,
      contentForReplacement: fuzzyContent
    };
  }
  function stripBom(content) {
    return content.startsWith("\uFEFF") ? { bom: "\uFEFF", text: content.slice(1) } : { bom: "", text: content };
  }
  function generateDiffString(oldContent, newContent, _contextLines = 4) {
    const oldLines = oldContent.split("\n");
    const newLines = newContent.split("\n");
    let firstChangedLine;
    for (let i = 0; i < Math.max(oldLines.length, newLines.length); i++) {
      if (oldLines[i] !== newLines[i]) {
        firstChangedLine = i + 1;
        break;
      }
    }
    const diff = `--- old
+++ new
@@ -1,${oldLines.length} +1,${newLines.length} @@
[Diff: ${oldLines.length} lines -> ${newLines.length} lines]`;
    return { diff, firstChangedLine };
  }

  // ../../packages/tools-fs/dist/edit.js
  var editSchema = Type.Object({
    path: Type.String({ description: "Path to the file to edit (relative to sandbox root)" }),
    oldText: Type.String({ description: "Exact text to find and replace (must match exactly)" }),
    newText: Type.String({ description: "New text to replace the old text with" })
  });
  function createEditTool(cwd, options) {
    const fs = getFileSystem();
    return {
      name: "edit",
      label: "edit",
      description: "Edit a file by replacing exact text. The oldText must match exactly (including whitespace). Use this for precise, surgical edits.",
      parameters: editSchema,
      execute: async (_toolCallId, params, signal) => {
        const { path, oldText, newText } = params;
        const absolutePath = resolveToCwd(path, cwd, options);
        return new Promise((resolve, reject) => {
          if (signal?.aborted) {
            reject(new Error("Operation aborted"));
            return;
          }
          let aborted = false;
          const onAbort = () => {
            aborted = true;
            reject(new Error("Operation aborted"));
          };
          if (signal) {
            signal.addEventListener("abort", onAbort, { once: true });
          }
          (async () => {
            try {
              try {
                await fs.access(absolutePath);
              } catch {
                if (signal)
                  signal.removeEventListener("abort", onAbort);
                reject(new Error(`File not found: ${path}`));
                return;
              }
              if (aborted)
                return;
              const rawContent = await fs.readFile(absolutePath, "utf-8");
              if (aborted)
                return;
              const { bom, text: content } = stripBom(rawContent);
              const originalEnding = detectLineEnding(content);
              const normalizedContent = normalizeToLF(content);
              const normalizedOldText = normalizeToLF(oldText);
              const normalizedNewText = normalizeToLF(newText);
              const matchResult = fuzzyFindText(normalizedContent, normalizedOldText);
              if (!matchResult.found) {
                if (signal)
                  signal.removeEventListener("abort", onAbort);
                reject(new Error(`Could not find the exact text in ${path}. The old text must match exactly including all whitespace and newlines.`));
                return;
              }
              const fuzzyContent = normalizedContent.replace(/\s+/g, " ");
              const fuzzyOldText = normalizedOldText.replace(/\s+/g, " ");
              const occurrences = fuzzyContent.split(fuzzyOldText).length - 1;
              if (occurrences > 1) {
                if (signal)
                  signal.removeEventListener("abort", onAbort);
                reject(new Error(`Found ${occurrences} occurrences of the text in ${path}. The text must be unique. Please provide more context to make it unique.`));
                return;
              }
              if (aborted)
                return;
              const baseContent = matchResult.contentForReplacement;
              const newContent = baseContent.substring(0, matchResult.index) + normalizedNewText + baseContent.substring(matchResult.index + matchResult.matchLength);
              if (baseContent === newContent) {
                if (signal)
                  signal.removeEventListener("abort", onAbort);
                reject(new Error(`No changes made to ${path}. The replacement produced identical content.`));
                return;
              }
              const finalContent = bom + restoreLineEndings(newContent, originalEnding);
              await fs.writeFile(absolutePath, finalContent, "utf-8");
              if (aborted)
                return;
              if (signal) {
                signal.removeEventListener("abort", onAbort);
              }
              const diffResult = generateDiffString(content, finalContent);
              resolve({
                content: [
                  {
                    type: "text",
                    text: `Successfully replaced text in ${path}.`
                  },
                  { type: "text", text: diffResult.diff }
                ],
                details: { diff: diffResult.diff, firstChangedLine: diffResult.firstChangedLine }
              });
            } catch (error) {
              if (signal) {
                signal.removeEventListener("abort", onAbort);
              }
              if (!aborted) {
                reject(error);
              }
            }
          })();
        });
      }
    };
  }

  // ../../packages/tools-fs/dist/ls.js
  var lsSchema = Type.Object({
    path: Type.Optional(Type.String({ description: "Directory to list (default: sandbox root)" })),
    limit: Type.Optional(Type.Number({ description: "Maximum number of entries to return (default: 500)" }))
  });
  var DEFAULT_LIMIT = 500;
  function createLsTool(cwd, options) {
    const fs = getFileSystem();
    return {
      name: "ls",
      label: "ls",
      description: `List directory contents. Returns entries sorted alphabetically, with '/' suffix for directories. Includes dotfiles. Output is truncated to ${DEFAULT_LIMIT} entries or ${DEFAULT_MAX_BYTES / 1024}KB (whichever is hit first).`,
      parameters: lsSchema,
      execute: async (_toolCallId, params, signal) => {
        return new Promise((resolve, reject) => {
          if (signal?.aborted) {
            reject(new Error("Operation aborted"));
            return;
          }
          const onAbort = () => reject(new Error("Operation aborted"));
          signal?.addEventListener("abort", onAbort, { once: true });
          (async () => {
            try {
              const { path: dirPath, limit } = params;
              const targetPath = resolveToCwd(dirPath || ".", cwd, options);
              const effectiveLimit = limit ?? DEFAULT_LIMIT;
              if (!await fs.exists(targetPath)) {
                reject(new Error(`Path not found: ${targetPath}`));
                return;
              }
              const stat = await fs.stat(targetPath);
              if (!stat.isDirectory) {
                reject(new Error(`Not a directory: ${targetPath}`));
                return;
              }
              let entries;
              try {
                entries = await fs.readdir(targetPath);
              } catch (e) {
                reject(new Error(`Cannot read directory: ${e.message}`));
                return;
              }
              entries.sort((a, b) => a.toLowerCase().localeCompare(b.toLowerCase()));
              const results = [];
              let entryLimitReached = false;
              for (const entry of entries) {
                if (results.length >= effectiveLimit) {
                  entryLimitReached = true;
                  break;
                }
                const fullPath = fs.join(targetPath, entry);
                let suffix = "";
                try {
                  const entryStat = await fs.stat(fullPath);
                  if (entryStat.isDirectory) {
                    suffix = "/";
                  }
                } catch {
                  continue;
                }
                results.push(entry + suffix);
              }
              signal?.removeEventListener("abort", onAbort);
              if (results.length === 0) {
                resolve({ content: [{ type: "text", text: "(empty directory)" }], details: void 0 });
                return;
              }
              const rawOutput = results.join("\n");
              const truncation = truncateHead(rawOutput, { maxLines: Number.MAX_SAFE_INTEGER });
              let output = truncation.content;
              const details = {};
              const notices = [];
              if (entryLimitReached) {
                notices.push(`${effectiveLimit} entries limit reached. Use limit=${effectiveLimit * 2} for more`);
                details.entryLimitReached = effectiveLimit;
              }
              if (truncation.truncated) {
                notices.push(`${formatSize(DEFAULT_MAX_BYTES)} limit reached`);
                details.truncation = truncation;
              }
              if (notices.length > 0) {
                output += `

[${notices.join(". ")}]`;
              }
              resolve({
                content: [{ type: "text", text: output }],
                details: Object.keys(details).length > 0 ? details : void 0
              });
            } catch (e) {
              signal?.removeEventListener("abort", onAbort);
              reject(e);
            }
          })();
        });
      }
    };
  }

  // ../../packages/tools-fs/dist/grep.js
  var grepSchema = Type.Object({
    pattern: Type.String({ description: "Search pattern (regex or literal string)" }),
    path: Type.Optional(Type.String({ description: "Directory or file to search (default: sandbox root)" })),
    glob: Type.Optional(Type.String({ description: "Filter files by glob pattern, e.g. '*.ts' or '**/*.js'" })),
    ignoreCase: Type.Optional(Type.Boolean({ description: "Case-insensitive search (default: false)" })),
    literal: Type.Optional(Type.Boolean({ description: "Treat pattern as literal string instead of regex (default: false)" })),
    context: Type.Optional(Type.Number({ description: "Number of lines to show before and after each match (default: 0)" })),
    limit: Type.Optional(Type.Number({ description: "Maximum number of matches to return (default: 100)" }))
  });

  // ../../packages/tools-fs/dist/find.js
  var findSchema = Type.Object({
    pattern: Type.String({
      description: "Glob pattern to match files, e.g. '*.ts', '**/*.json', or 'src/**/*.spec.ts'"
    }),
    path: Type.Optional(Type.String({ description: "Directory to search in (default: sandbox root)" })),
    limit: Type.Optional(Type.Number({ description: "Maximum number of results (default: 1000)" }))
  });

  // src/unity-http-client.ts
  function sleep2(ms) {
    return new Promise((resolve) => setTimeout(resolve, ms));
  }
  var sseBuffers = /* @__PURE__ */ new Map();
  globalThis._pieSSEPush = (requestId, line) => {
    let buf = sseBuffers.get(requestId);
    if (!buf) {
      buf = { lines: [] };
      sseBuffers.set(requestId, buf);
    }
    buf.lines.push(line);
    if (buf.resolve) {
      const r = buf.resolve;
      buf.resolve = void 0;
      r();
    }
  };
  globalThis._pieSSEStatus = (requestId, status, error) => {
    let buf = sseBuffers.get(requestId);
    if (!buf) {
      buf = { lines: [] };
      sseBuffers.set(requestId, buf);
    }
    buf.httpStatus = status;
    buf.httpError = error;
    if (buf.resolve) {
      const r = buf.resolve;
      buf.resolve = void 0;
      r();
    }
  };
  function waitForData(requestId) {
    const buf = sseBuffers.get(requestId);
    if (!buf) return Promise.resolve();
    if (buf.lines.length > 0) return Promise.resolve();
    return new Promise((resolve) => {
      buf.resolve = resolve;
    });
  }
  function waitForStatus(requestId) {
    const buf = sseBuffers.get(requestId);
    if (!buf) return Promise.resolve();
    if (buf.httpStatus !== void 0) return Promise.resolve();
    return new Promise((resolve) => {
      buf.resolve = resolve;
    });
  }
  var UnityHttpClient = class _UnityHttpClient {
    static MAX_RETRIES = 5;
    static RETRYABLE_STATUS = /* @__PURE__ */ new Set([429, 500, 502, 503, 504]);
    async request(url, options = {}) {
      const method = options.method || "POST";
      const body = options.body || "";
      const headers = options.headers || {};
      const authHeader = headers["Authorization"] || headers["authorization"] || "";
      const bearer = authHeader.replace(/^Bearer\s+/i, "");
      const extraHeaders = {};
      for (const [k, v] of Object.entries(headers)) {
        if (k.toLowerCase() === "authorization") continue;
        if (k.toLowerCase() === "content-type") continue;
        extraHeaders[k] = v;
      }
      const headersJson = Object.keys(extraHeaders).length > 0 ? JSON.stringify(extraHeaders) : null;
      let requestId = -1;
      let httpStatus = -1;
      for (let attempt = 0; attempt <= _UnityHttpClient.MAX_RETRIES; attempt++) {
        requestId = CS.Pie.PieHttpBridge.StartStreamRequest(
          method,
          url,
          method === "GET" ? "" : body,
          bearer,
          headersJson
        );
        sseBuffers.set(requestId, { lines: [] });
        const headerTimeoutMs = 6e4;
        const headerStart = Date.now();
        while (true) {
          const buf = sseBuffers.get(requestId);
          if (buf.httpStatus !== void 0) {
            httpStatus = buf.httpStatus;
            break;
          }
          if (Date.now() - headerStart > headerTimeoutMs) {
            CS.Pie.PieHttpBridge.CancelRequest(requestId);
            sseBuffers.delete(requestId);
            throw new Error("Timed out waiting for HTTP response headers");
          }
          await waitForStatus(requestId);
        }
        if (httpStatus >= 200 && httpStatus < 300) break;
        if (_UnityHttpClient.RETRYABLE_STATUS.has(httpStatus) && attempt < _UnityHttpClient.MAX_RETRIES) {
          const errorBody2 = sseBuffers.get(requestId)?.httpError ?? "";
          CS.Pie.PieHttpBridge.CancelRequest(requestId);
          sseBuffers.delete(requestId);
          let retryAfterMs = 0;
          const retryMatch = errorBody2.match(/"retry.after":\s*(\d+)/i);
          if (retryMatch) retryAfterMs = parseInt(retryMatch[1], 10) * 1e3;
          const backoffMs = Math.max(retryAfterMs, 2 ** attempt * 1e3 + Math.random() * 500);
          globalThis.pieBridge?.log("warn", `[UnityHttpClient] ${httpStatus} \u2013 retrying in ${Math.round(backoffMs)}ms (attempt ${attempt + 1}/${_UnityHttpClient.MAX_RETRIES})`);
          await sleep2(backoffMs);
          continue;
        }
        const errorBody = sseBuffers.get(requestId)?.httpError ?? `HTTP ${httpStatus}`;
        CS.Pie.PieHttpBridge.CancelRequest(requestId);
        sseBuffers.delete(requestId);
        const err = new Error(errorBody);
        err.status = httpStatus;
        err.statusCode = httpStatus;
        throw err;
      }
      const capturedRequestId = requestId;
      const response = {
        status: httpStatus,
        statusText: httpStatus >= 200 && httpStatus < 300 ? "OK" : "Error",
        ok: httpStatus >= 200 && httpStatus < 300,
        headers: { "content-type": "application/json" },
        async text() {
          const lines = [];
          for await (const line of response.readSSELines()) {
            lines.push(line);
          }
          return lines.join("\n");
        },
        async json() {
          return JSON.parse(await response.text());
        },
        async *readSSELines(signal) {
          const maxWaitMs = 18e4;
          const startTime = Date.now();
          try {
            while (true) {
              if (signal?.aborted) {
                CS.Pie.PieHttpBridge.CancelRequest(capturedRequestId);
                throw new Error("Request aborted");
              }
              if (Date.now() - startTime > maxWaitMs) {
                CS.Pie.PieHttpBridge.CancelRequest(capturedRequestId);
                throw new Error("Request timed out");
              }
              const buf = sseBuffers.get(capturedRequestId);
              if (!buf) return;
              while (buf.lines.length > 0) {
                const line = buf.lines.shift();
                if (line === null) return;
                yield line;
              }
              await waitForData(capturedRequestId);
            }
          } finally {
            sseBuffers.delete(capturedRequestId);
          }
        }
      };
      return response;
    }
  };

  // src/tools/eval-js.ts
  function createEvalJsTool() {
    return {
      name: "eval_js",
      label: "eval_js",
      description: `Execute JavaScript code in the Unity PuerTS runtime (V8 engine).
The code can access Unity C# APIs via the global \`CS\` object.

Common patterns:
  // Create GameObject
  new CS.UnityEngine.GameObject("MyObject")

  // Add component
  const go = CS.UnityEngine.GameObject.Find("MyObject");
  go.AddComponent(puer.$typeof(CS.UnityEngine.BoxCollider));

  // Transform
  go.transform.position = new CS.UnityEngine.Vector3(1, 2, 3);

  // Find objects
  CS.UnityEngine.Object.FindObjectsOfType(puer.$typeof(CS.UnityEngine.Camera));

  // Materials
  const renderer = go.GetComponent(puer.$typeof(CS.UnityEngine.MeshRenderer));
  renderer.sharedMaterial.color = new CS.UnityEngine.Color(1, 0, 0, 1);

  // Primitives
  CS.UnityEngine.GameObject.CreatePrimitive(CS.UnityEngine.PrimitiveType.Cube);

  // Editor-only API (Editor mode only)
  CS.UnityEditor.Selection.activeGameObject = go;

The last expression value is returned as the tool result.
Use JSON.stringify() for complex data.`,
      parameters: Type.Object({
        code: Type.String({ description: "JavaScript code to execute in Unity PuerTS runtime" })
      }),
      execute: async (_toolCallId, params) => {
        try {
          const result = (0, eval)(params.code);
          const finalResult = result instanceof Promise ? await result : result;
          let text;
          if (finalResult === void 0 || finalResult === null) {
            text = "OK (no return value)";
          } else if (typeof finalResult === "object") {
            try {
              text = JSON.stringify(finalResult, null, 2);
            } catch {
              text = String(finalResult);
            }
          } else {
            text = String(finalResult);
          }
          return {
            content: [{ type: "text", text }],
            details: {}
          };
        } catch (e) {
          return {
            content: [{ type: "text", text: `Error: ${e?.message ?? e}
${e?.stack ?? ""}` }],
            details: {},
            isError: true
          };
        }
      }
    };
  }

  // src/tools/manage-todo-list.ts
  var TodoItemSchema = Type.Object({
    id: Type.Number({ description: "Unique identifier for the todo. Use sequential numbers starting from 1." }),
    title: Type.String({ description: "Concise action-oriented todo label (3-7 words)." }),
    description: Type.String({ description: "Detailed context or implementation notes." }),
    status: Type.String({
      description: "Progress state. Preferred values: not-started, in-progress, completed. Also accepts common aliases like pending, todo, doing, done."
    })
  });
  var ManageTodoListParams = Type.Object({
    operation: Type.Union([Type.Literal("write"), Type.Literal("read")], {
      description: "write: Replace the entire todo list. read: Return the current todo list."
    }),
    todoList: Type.Optional(
      Type.Array(TodoItemSchema, {
        description: "Required for write. Must include the full todo list with sequential ids starting from 1."
      })
    )
  });
  var TOOL_DESCRIPTION = `Manage a structured todo list for multi-step work.

Use this tool when a task benefits from planning, progress tracking, or step-by-step execution.
Always write the full list when updating it.

Preferred status values:
- not-started
- in-progress
- completed`;
  function cloneTodos(todos) {
    return todos.map((todo) => ({ ...todo }));
  }
  function normalizeStatus(status) {
    const value = String(status || "").trim().toLowerCase();
    if (!value) return null;
    if (value === "not-started" || value === "not_started" || value === "pending" || value === "todo" || value === "not started") {
      return "not-started";
    }
    if (value === "in-progress" || value === "in_progress" || value === "in progress" || value === "doing" || value === "active" || value === "working") {
      return "in-progress";
    }
    if (value === "completed" || value === "complete" || value === "done" || value === "finished") {
      return "completed";
    }
    return null;
  }
  function normalizeTodos(todos) {
    return todos.map((todo) => ({
      ...todo,
      status: normalizeStatus(todo.status) ?? todo.status
    }));
  }
  function validateTodos(todos) {
    const errors = [];
    const validStatuses = /* @__PURE__ */ new Set(["not-started", "in-progress", "completed"]);
    for (let index = 0; index < todos.length; index++) {
      const item = todos[index];
      const prefix = `Item ${index + 1}`;
      if (typeof item?.id !== "number" || !Number.isInteger(item.id) || item.id < 1) {
        errors.push(`${prefix}: 'id' must be a positive integer`);
      }
      if (item?.id !== index + 1) {
        errors.push(`${prefix}: ids must be sequential starting from 1`);
      }
      if (typeof item?.title !== "string" || item.title.trim().length === 0) {
        errors.push(`${prefix}: 'title' is required`);
      }
      if (typeof item?.description !== "string") {
        errors.push(`${prefix}: 'description' must be a string`);
      }
      if (!validStatuses.has(item?.status)) {
        errors.push(`${prefix}: 'status' must be one of: not-started, in-progress, completed`);
      }
    }
    return errors;
  }
  function createManageTodoListTool() {
    let todos = [];
    const tool = {
      name: "manage_todo_list",
      label: "manage_todo_list",
      description: TOOL_DESCRIPTION,
      parameters: ManageTodoListParams,
      execute: async (_toolCallId, args) => {
        if (args.operation === "read") {
          return {
            content: [
              {
                type: "text",
                text: todos.length ? JSON.stringify(todos, null, 2) : "No todos. Use write operation to create a todo list."
              }
            ],
            details: { operation: "read", todos: cloneTodos(todos) }
          };
        }
        if (!Array.isArray(args.todoList)) {
          return {
            content: [{ type: "text", text: "Error: todoList is required for write operation." }],
            details: { operation: "write", todos: cloneTodos(todos), error: "todoList required" },
            isError: true
          };
        }
        const nextTodos = normalizeTodos(cloneTodos(args.todoList));
        const errors = validateTodos(nextTodos);
        if (errors.length > 0) {
          return {
            content: [{ type: "text", text: `Validation failed:
${errors.map((error) => `  - ${error}`).join("\n")}` }],
            details: { operation: "write", todos: cloneTodos(todos), error: errors.join("; ") },
            isError: true
          };
        }
        todos = nextTodos;
        const completed = todos.filter((todo) => todo.status === "completed").length;
        return {
          content: [{
            type: "text",
            text: `Todos updated successfully. ${completed}/${todos.length} completed.`
          }],
          details: { operation: "write", todos: cloneTodos(todos) }
        };
      }
    };
    return {
      tool,
      getState() {
        return cloneTodos(todos);
      },
      restore(state) {
        if (!Array.isArray(state)) {
          todos = [];
          return;
        }
        const normalized = normalizeTodos(cloneTodos(state));
        const errors = validateTodos(normalized);
        todos = errors.length === 0 ? normalized : [];
      },
      reset() {
        todos = [];
      }
    };
  }

  // src/tools/native-find.ts
  var findSchema2 = Type.Object({
    pattern: Type.Optional(Type.String({
      description: "The file pattern only, not a shell command. Good inputs: '*.cs', '**/*.json', 'DemoScript.cs', or 'DemoScript'. Do not pass 'find *.cs' as the pattern."
    })),
    query: Type.Optional(Type.String({ description: "Alias for pattern. You can pass a filename fragment like 'DemoScript'." })),
    name: Type.Optional(Type.String({ description: "Alias for pattern. You can pass a file name like 'DemoScript.cs'." })),
    file_name: Type.Optional(Type.String({ description: "Alias for pattern. You can pass a file name or fragment." })),
    path: Type.Optional(Type.String({ description: "Directory to search in (default: workspace root). Example: 'Scripts' or 'Data'." })),
    limit: Type.Optional(Type.Number({ description: "Maximum number of results (default: 1000)" }))
  });
  var DEFAULT_LIMIT2 = 1e3;
  function awaitTask(task) {
    if (!puer?.$promise) {
      throw new Error("puer.$promise is not available");
    }
    return puer.$promise(task);
  }
  function stripOuterQuotes(value) {
    const trimmed = (value || "").trim();
    if (trimmed.length < 2) {
      return trimmed;
    }
    const first = trimmed[0];
    const last = trimmed[trimmed.length - 1];
    if (first === '"' && last === '"' || first === "'" && last === "'") {
      return trimmed.substring(1, trimmed.length - 1).trim();
    }
    return trimmed;
  }
  function normalizeFindPattern(rawPattern) {
    let pattern = stripOuterQuotes(rawPattern);
    if (pattern.startsWith("find ")) {
      pattern = pattern.substring(5).trim();
    }
    if (pattern.startsWith("-name ")) {
      pattern = pattern.substring(6).trim();
    }
    const nameMatch = pattern.match(/^-name\s+(.+)$/);
    if (nameMatch) {
      pattern = nameMatch[1].trim();
    }
    pattern = stripOuterQuotes(pattern);
    if (pattern.startsWith("./")) {
      pattern = pattern.substring(2);
    }
    return pattern;
  }
  function createNativeFindTool(cwd, options) {
    return {
      name: "find",
      label: "find",
      description: `Search for files by filename fragment or glob pattern. Best practice: use a simple filename fragment like 'PlayerController' or a direct filename like 'PlayerController.cs'. Glob examples: '*.cs', '*.json', '**/*.prefab'. Pass only the pattern string, not a shell command. Returns matching paths relative to the search directory. Output is truncated to ${DEFAULT_LIMIT2} results or ${DEFAULT_MAX_BYTES / 1024}KB (whichever is hit first).`,
      parameters: findSchema2,
      execute: async (_toolCallId, { pattern, query, name, file_name, path: searchDir, limit }) => {
        const resolvedPattern = pattern || query || name || file_name || "";
        const normalizedPattern = normalizeFindPattern(resolvedPattern);
        if (!normalizedPattern) {
          return {
            content: [{ type: "text", text: "search_files requires a pattern, query, name, or file_name argument." }],
            details: { rawPattern: resolvedPattern, effectivePattern: normalizedPattern },
            isError: true
          };
        }
        const searchPath = resolveToCwd(searchDir || ".", cwd, options);
        const effectiveLimit = limit ?? DEFAULT_LIMIT2;
        if (globalThis.__pieVerboseLogs) {
          globalThis.pieBridge?.log?.("info", `[native find] path=${searchPath} pattern=${normalizedPattern} raw=${resolvedPattern} limit=${effectiveLimit}`);
        }
        const json = await awaitTask(CS.Pie.PieFileBridge.FindAsync(searchPath, normalizedPattern, effectiveLimit));
        const nativeResult = json ? JSON.parse(json) : {};
        const results = nativeResult.Results ?? [];
        if (globalThis.__pieVerboseLogs) {
          globalThis.pieBridge?.log?.(
            "info",
            `[native find] done path=${searchPath} pattern=${normalizedPattern} matches=${results.length} dirs=${nativeResult.ScannedDirectories ?? 0} files=${nativeResult.ScannedFiles ?? 0}`
          );
        }
        if (results.length === 0) {
          return {
            content: [{ type: "text", text: "No files found matching pattern" }],
            details: {
              scannedDirectories: nativeResult.ScannedDirectories ?? 0,
              scannedFiles: nativeResult.ScannedFiles ?? 0,
              effectivePattern: nativeResult.Pattern ?? normalizedPattern,
              searchPath: nativeResult.RootPath ?? searchPath,
              rawPattern: resolvedPattern
            },
            isError: true
          };
        }
        const rawOutput = results.join("\n");
        const truncation = truncateHead(rawOutput, { maxLines: Number.MAX_SAFE_INTEGER });
        let resultOutput = truncation.content;
        const details = {
          scannedDirectories: nativeResult.ScannedDirectories,
          scannedFiles: nativeResult.ScannedFiles,
          effectivePattern: nativeResult.Pattern ?? normalizedPattern,
          searchPath: nativeResult.RootPath ?? searchPath,
          rawPattern: resolvedPattern
        };
        const notices = [];
        if (nativeResult.LimitReached) {
          notices.push(`${effectiveLimit} results limit reached`);
          details.resultLimitReached = effectiveLimit;
        }
        if (truncation.truncated) {
          notices.push(`${formatSize(DEFAULT_MAX_BYTES)} limit reached`);
          details.truncation = truncation;
        }
        if (notices.length > 0) {
          resultOutput += `

[${notices.join(". ")}]`;
        }
        return {
          content: [{ type: "text", text: resultOutput }],
          details
        };
      }
    };
  }

  // src/tools/native-grep.ts
  var grepSchema2 = Type.Object({
    pattern: Type.String({ description: "The text or regex pattern to search for inside files. Example literal patterns: 'MonoBehaviour', 'Hello from Pie'. Example regex: 'class\\s+DemoScript'." }),
    path: Type.Optional(Type.String({ description: "Directory or file to search (default: workspace root). Example: 'Scripts' or 'Data/dialogue'." })),
    glob: Type.Optional(Type.String({ description: "Optional file filter, e.g. '*.cs', '*.json', or '**/*.txt'. Use this to narrow which files are searched." })),
    ignoreCase: Type.Optional(Type.Boolean({ description: "Case-insensitive search (default: false)" })),
    literal: Type.Optional(Type.Boolean({ description: "Treat pattern as plain text instead of regex (default: false). Use literal=true for normal substring searches like 'MonoBehaviour'." })),
    context: Type.Optional(Type.Number({ description: "Number of lines to show before and after each match (default: 0)" })),
    limit: Type.Optional(Type.Number({ description: "Maximum number of matches to return (default: 100)" }))
  });
  var DEFAULT_LIMIT3 = 100;
  function awaitTask2(task) {
    if (!puer?.$promise) {
      throw new Error("puer.$promise is not available");
    }
    return puer.$promise(task);
  }
  function createNativeGrepTool(cwd, options) {
    return {
      name: "grep",
      label: "grep",
      description: `Search inside file contents. Use this when you know some text that should appear in the file. Common usage: pattern='MonoBehaviour', glob='*.cs', literal=true. Prefer targeted folders like 'Scripts' or 'Data' over scanning the whole project. Returns matching lines with file paths and line numbers. Output is truncated to ${DEFAULT_LIMIT3} matches or ${DEFAULT_MAX_BYTES / 1024}KB (whichever is hit first). Long lines are truncated to ${GREP_MAX_LINE_LENGTH} chars.`,
      parameters: grepSchema2,
      execute: async (_toolCallId, { pattern, path: searchDir, glob, ignoreCase, literal, context, limit }) => {
        const searchPath = resolveToCwd(searchDir || ".", cwd, options);
        const effectiveLimit = Math.max(1, limit ?? DEFAULT_LIMIT3);
        const ctx = context && context > 0 ? context : 0;
        if (globalThis.__pieVerboseLogs) {
          globalThis.pieBridge?.log?.(
            "info",
            `[native grep] path=${searchPath} pattern=${pattern} glob=${glob ?? ""} limit=${effectiveLimit}`
          );
        }
        const json = await awaitTask2(
          CS.Pie.PieFileBridge.GrepAsync(
            searchPath,
            pattern,
            glob ?? "",
            !!ignoreCase,
            !!literal,
            ctx,
            effectiveLimit
          )
        );
        const nativeResult = json ? JSON.parse(json) : {};
        const outputLines = nativeResult.Lines ?? [];
        const matchCount = nativeResult.MatchCount ?? 0;
        if (globalThis.__pieVerboseLogs) {
          globalThis.pieBridge?.log?.(
            "info",
            `[native grep] done path=${searchPath} pattern=${pattern} matches=${matchCount} files=${nativeResult.FilesScanned ?? 0}`
          );
        }
        if (matchCount === 0) {
          return {
            content: [{ type: "text", text: "No matches found" }],
            details: {
              filesScanned: nativeResult.FilesScanned ?? 0,
              effectivePattern: nativeResult.Pattern ?? pattern,
              searchPath: nativeResult.SearchPath ?? searchPath,
              glob: nativeResult.Glob ?? glob,
              literal: nativeResult.Literal ?? !!literal,
              ignoreCase: nativeResult.IgnoreCase ?? !!ignoreCase
            },
            isError: true
          };
        }
        const rawOutput = outputLines.join("\n");
        const truncation = truncateHead(rawOutput, { maxLines: Number.MAX_SAFE_INTEGER });
        let output = truncation.content;
        const details = {
          filesScanned: nativeResult.FilesScanned,
          effectivePattern: nativeResult.Pattern ?? pattern,
          searchPath: nativeResult.SearchPath ?? searchPath,
          glob: nativeResult.Glob ?? glob,
          literal: nativeResult.Literal ?? !!literal,
          ignoreCase: nativeResult.IgnoreCase ?? !!ignoreCase
        };
        const notices = [];
        if (nativeResult.MatchLimitReached) {
          notices.push(`${effectiveLimit} matches limit reached`);
          details.matchLimitReached = effectiveLimit;
        }
        if (truncation.truncated) {
          notices.push(`${formatSize(DEFAULT_MAX_BYTES)} limit reached`);
          details.truncation = truncation;
        }
        if (nativeResult.LinesTruncated) {
          notices.push(`lines longer than ${GREP_MAX_LINE_LENGTH} chars were truncated`);
          details.linesTruncated = true;
        }
        if (notices.length > 0) {
          output += `

[${notices.join(". ")}]`;
        }
        return {
          content: [{ type: "text", text: output }],
          details
        };
      }
    };
  }

  // src/skills.ts
  var BUILTIN_SKILLS = [];
  function normalizePath(path) {
    return String(path ?? "").replace(/\\/g, "/");
  }
  function parseBridgePathArray(methodName) {
    const raw = globalThis.pieBridge?.[methodName]?.();
    if (typeof raw !== "string" || raw.trim().length === 0) {
      return [];
    }
    try {
      const parsed = JSON.parse(raw);
      return Array.isArray(parsed) ? parsed.map((entry) => normalizePath(String(entry ?? ""))).filter((entry) => entry.length > 0) : [];
    } catch {
      return [];
    }
  }
  function getSkillSearchPaths(_projectRoot) {
    return parseBridgePathArray("getSkillSearchPathsJson");
  }
  function getExtensionSearchPaths(_projectRoot) {
    return parseBridgePathArray("getExtensionSearchPathsJson");
  }
  function parseSkillDescription(content) {
    const frontmatterMatch = content.match(/^---\n[\s\S]*?description:\s*["']?(.+?)["']?(?:\n|$)/m);
    if (frontmatterMatch) return frontmatterMatch[1].trim();
    const headingMatch = content.match(/^#\s+(.+)$/m);
    if (headingMatch) return headingMatch[1].trim();
    for (const line of content.split("\n")) {
      const trimmed = line.trim();
      if (trimmed && !trimmed.startsWith("---")) {
        return trimmed.slice(0, 100);
      }
    }
    return "No description";
  }
  function loadProjectSkills(projectRoot2) {
    const loaded = /* @__PURE__ */ new Map();
    const skillDirs = getSkillSearchPaths(projectRoot2);
    for (const skillsDir of skillDirs) {
      if (!CS.System.IO.Directory.Exists(skillsDir)) continue;
      const directories = CS.System.IO.Directory.GetDirectories(skillsDir);
      for (let i = 0; i < directories.Length; i++) {
        const dir = directories[i];
        const name = String(CS.System.IO.Path.GetFileName(dir) ?? "");
        const skillPath = CS.System.IO.Path.Combine(dir, "SKILL.md");
        if (!name || !CS.System.IO.File.Exists(skillPath)) continue;
        const content = String(CS.System.IO.File.ReadAllText(skillPath) ?? "");
        loaded.delete(name);
        loaded.set(name, {
          name,
          description: parseSkillDescription(content),
          source: "project",
          path: normalizePath(String(skillPath))
        });
      }
      const files = CS.System.IO.Directory.GetFiles(skillsDir, "*.md");
      for (let i = 0; i < files.Length; i++) {
        const filePath = files[i];
        const fileName = String(CS.System.IO.Path.GetFileNameWithoutExtension(filePath) ?? "");
        if (!fileName) continue;
        const content = String(CS.System.IO.File.ReadAllText(filePath) ?? "");
        loaded.delete(fileName);
        loaded.set(fileName, {
          name: fileName,
          description: parseSkillDescription(content),
          source: "project",
          path: normalizePath(String(filePath))
        });
      }
    }
    return Array.from(loaded.values());
  }
  function getAvailableSkills(projectRoot2) {
    const skillMap = /* @__PURE__ */ new Map();
    for (const skill of BUILTIN_SKILLS) {
      skillMap.set(skill.name, skill);
    }
    for (const skill of loadProjectSkills(projectRoot2)) {
      skillMap.set(skill.name, skill);
    }
    return Array.from(skillMap.values());
  }
  function formatSkillsForPrompt(skills) {
    if (!skills || skills.length === 0) {
      return "";
    }
    const lines = skills.slice().sort((a, b) => a.name.localeCompare(b.name)).map((skill) => `- ${skill.name} [${skill.source}] - ${skill.description}`);
    return [
      "Project skills are available via the read_skill tool.",
      "Read a skill before relying on project-specific workflows or conventions.",
      "[Available Skills]",
      ...lines
    ].join("\n");
  }
  function readSkillContent(projectRoot2, name) {
    const skills = getAvailableSkills(projectRoot2);
    const match = skills.find((skill) => skill.name === name);
    if (!match) return null;
    if (match.source === "builtin") {
      return match;
    }
    if (!match.path || !CS.System.IO.File.Exists(match.path)) {
      return null;
    }
    return {
      ...match,
      content: CS.System.IO.File.ReadAllText(match.path)
    };
  }

  // src/tools/inspect-unity-context.ts
  function getRenderPipelineInfo() {
    const graphicsSettings = CS.UnityEngine.Rendering.GraphicsSettings;
    const qualitySettings = CS.UnityEngine.QualitySettings;
    const globalAsset = graphicsSettings.renderPipelineAsset;
    const qualityAsset = qualitySettings.renderPipeline;
    const activeAsset = qualityAsset || globalAsset;
    const assetName = activeAsset ? activeAsset.GetType().FullName : "";
    let pipeline = "Built-in Render Pipeline";
    if (assetName.indexOf("Universal") >= 0) {
      pipeline = "Universal Render Pipeline";
    } else if (assetName.indexOf("HDRender") >= 0 || assetName.indexOf("HighDefinition") >= 0) {
      pipeline = "High Definition Render Pipeline";
    }
    return {
      pipeline,
      assetType: assetName || "None",
      hasRenderPipelineAsset: !!activeAsset
    };
  }
  function detectPackageHints(projectRoot2) {
    const manifestPath = CS.System.IO.Path.Combine(projectRoot2, "Packages", "manifest.json");
    if (!CS.System.IO.File.Exists(manifestPath)) return [];
    try {
      const content = String(CS.System.IO.File.ReadAllText(manifestPath));
      const hints = [];
      if (content.indexOf("com.unity.render-pipelines.universal") >= 0) hints.push("URP package");
      if (content.indexOf("com.unity.render-pipelines.high-definition") >= 0) hints.push("HDRP package");
      if (content.indexOf("com.unity.render-pipelines.core") >= 0) hints.push("SRP core package");
      return hints;
    } catch {
      return [];
    }
  }
  function getUnityContextSnapshot(projectRoot2, isEditor2) {
    const pipelineInfo = getRenderPipelineInfo();
    const sceneManager = CS.UnityEngine.SceneManagement.SceneManager;
    const activeScene = sceneManager.GetActiveScene();
    const selection = isEditor2 && CS.UnityEditor && CS.UnityEditor.Selection ? CS.UnityEditor.Selection.activeGameObject : null;
    const agentsPath = globalThis.pieBridge?.getProjectMemoryPath?.() || CS.System.IO.Path.Combine(projectRoot2, "Assets", "Pie", "AGENTS.md");
    const roots = activeScene && activeScene.IsValid() ? activeScene.GetRootGameObjects() : [];
    const rootObjects = [];
    for (let i = 0; i < roots.length; i++) {
      rootObjects.push(String(roots[i].name));
    }
    return {
      projectRoot: projectRoot2,
      isEditor: isEditor2,
      sceneName: activeScene ? activeScene.name : "",
      scenePath: activeScene ? activeScene.path : "",
      selectedObject: selection ? selection.name : "",
      colorSpace: String(CS.UnityEngine.QualitySettings.activeColorSpace),
      productName: CS.UnityEngine.Application.productName,
      unityVersion: CS.UnityEngine.Application.unityVersion,
      agentsPath,
      hasProjectMemory: CS.System.IO.File.Exists(agentsPath),
      renderPipeline: pipelineInfo.pipeline,
      renderPipelineAssetType: pipelineInfo.assetType,
      packageHints: detectPackageHints(projectRoot2),
      rootObjectCount: rootObjects.length,
      rootObjects: rootObjects.slice(0, 12)
    };
  }

  // src/tools/project-memory.ts
  function getAgentsPath(projectRoot2) {
    const fromBridge = globalThis.pieBridge?.getProjectMemoryPath?.();
    if (typeof fromBridge === "string" && fromBridge.length > 0) {
      return fromBridge;
    }
    return CS.System.IO.Path.Combine(projectRoot2, "Assets", "Pie", "AGENTS.md");
  }
  function ensureProjectMemoryDir(projectRoot2) {
    const agentsPath = getAgentsPath(projectRoot2);
    const dir = CS.System.IO.Path.GetDirectoryName(agentsPath);
    if (dir && !CS.System.IO.Directory.Exists(dir)) {
      CS.System.IO.Directory.CreateDirectory(dir);
    }
  }
  function readProjectMemory(projectRoot2) {
    const agentsPath = getAgentsPath(projectRoot2);
    if (!CS.System.IO.File.Exists(agentsPath)) return "";
    return CS.System.IO.File.ReadAllText(agentsPath);
  }
  function getProjectMemoryExcerpt(projectRoot2, maxChars = 8e3) {
    const content = readProjectMemory(projectRoot2);
    if (!content) return "";
    return content.length > maxChars ? content.slice(0, maxChars) + "\n\n... (truncated)" : content;
  }
  function createWriteProjectMemoryTool(projectRoot2) {
    return {
      name: "write_project_memory",
      label: "write_project_memory",
      description: "Create or overwrite Assets/Pie/AGENTS.md with persistent project instructions and facts.",
      parameters: Type.Object({
        content: Type.String({ description: "Full markdown contents for Assets/Pie/AGENTS.md." })
      }),
      execute: async (_toolCallId, params) => {
        ensureProjectMemoryDir(projectRoot2);
        const agentsPath = getAgentsPath(projectRoot2);
        CS.System.IO.File.WriteAllText(agentsPath, params.content || "");
        return {
          content: [{
            type: "text",
            text: `Project memory written to ${agentsPath}`
          }],
          details: { path: agentsPath, length: (params.content || "").length }
        };
      }
    };
  }
  function buildInitialProjectMemory(snapshot) {
    const packageLines = snapshot.packageHints.length > 0 ? snapshot.packageHints.map((hint) => `- ${hint}`).join("\n") : "- No SRP package hints detected";
    const rootObjectLines = snapshot.rootObjects.length > 0 ? snapshot.rootObjects.map((name) => `- ${name}`).join("\n") : "- No root objects detected";
    return `# AGENTS.md instructions

## Project Overview
- Product: ${snapshot.productName}
- Unity Version: ${snapshot.unityVersion}
- Scene: ${snapshot.sceneName || "(unknown)"}
- Project Root: ${snapshot.projectRoot}

## Render Pipeline
- Active pipeline: ${snapshot.renderPipeline}
- Pipeline asset type: ${snapshot.renderPipelineAssetType}
- Color space: ${snapshot.colorSpace}

## Package Hints
${packageLines}

## Scene Snapshot
- Root object count: ${snapshot.rootObjectCount}
${rootObjectLines}

## Safe Editing Rules
- Always inspect Unity context before creating materials or shaders.
- Never assume Built-in, URP, or HDRP from memory alone.
- Prefer reusing existing scene shaders/materials when possible.
- Keep scene edits small and observable.

## Validation Checklist
- Confirm render pipeline before material creation.
- Confirm target scene object names before mutation.
- Summarize created or modified objects after each task.
`;
  }

  // src/session-store.ts
  function isSessionEmpty(record) {
    if (!record) return true;
    const messageCount = Array.isArray(record.messages) ? record.messages.length : 0;
    const todoCount = Array.isArray(record.todoState) ? record.todoState.length : 0;
    return messageCount === 0 && todoCount === 0;
  }
  function getSessionsDir(projectRoot2) {
    const fromBridge = globalThis.pieBridge?.getSessionsDirectory?.();
    if (typeof fromBridge === "string" && fromBridge.length > 0) {
      return fromBridge;
    }
    throw new Error("Pie session storage path is unavailable. pieBridge.getSessionsDirectory() must be provided by the host.");
  }
  function ensureSessionsDir(projectRoot2) {
    const dir = getSessionsDir(projectRoot2);
    if (!CS.System.IO.Directory.Exists(dir)) {
      CS.System.IO.Directory.CreateDirectory(dir);
    }
    return dir;
  }
  function getSessionPath(projectRoot2, sessionId) {
    return CS.System.IO.Path.Combine(getSessionsDir(projectRoot2), `${sessionId}.json`);
  }
  function isoNow() {
    return (/* @__PURE__ */ new Date()).toISOString();
  }
  function createSessionId() {
    return `sess_${Date.now().toString(36)}_${Math.random().toString(36).slice(2, 8)}`;
  }
  function deriveTitle(messages) {
    const firstUser = messages.find((message) => message?.role === "user");
    if (!firstUser) return "New Session";
    const content = Array.isArray(firstUser.content) ? firstUser.content.filter((block) => block?.type === "text").map((block) => block.text || "").join(" ") : "";
    const compact = String(content || "New Session").replace(/\s+/g, " ").trim();
    return compact.slice(0, 48) || "New Session";
  }
  function listSessionFiles(projectRoot2) {
    const dir = ensureSessionsDir(projectRoot2);
    const files = CS.System.IO.Directory.GetFiles(dir, "*.json");
    const result = [];
    for (let i = 0; i < files.Length; i++) {
      result.push(files[i]);
    }
    return result;
  }
  function createSessionRecord(params) {
    const now = isoNow();
    return {
      id: params.sessionId || createSessionId(),
      title: deriveTitle(params.messages),
      createdAt: now,
      updatedAt: now,
      parentSessionId: params.parentSessionId,
      provider: params.provider,
      modelId: params.modelId,
      messageCount: params.messages.length,
      messages: params.messages,
      todoState: params.todoState
    };
  }
  function saveSession(projectRoot2, record) {
    ensureSessionsDir(projectRoot2);
    let createdAt = record.createdAt;
    const existing = loadSession(projectRoot2, record.id);
    if (existing?.createdAt) {
      createdAt = existing.createdAt;
    }
    const normalized = {
      ...record,
      createdAt,
      updatedAt: isoNow(),
      title: deriveTitle(record.messages),
      messageCount: record.messages.length
    };
    CS.System.IO.File.WriteAllText(
      getSessionPath(projectRoot2, normalized.id),
      JSON.stringify(normalized, null, 2)
    );
    return normalized;
  }
  function deleteSession(projectRoot2, sessionId) {
    const path = getSessionPath(projectRoot2, sessionId);
    if (CS.System.IO.File.Exists(path)) {
      CS.System.IO.File.Delete(path);
    }
  }
  function loadSession(projectRoot2, sessionId) {
    const path = getSessionPath(projectRoot2, sessionId);
    if (!CS.System.IO.File.Exists(path)) return null;
    try {
      return JSON.parse(CS.System.IO.File.ReadAllText(path));
    } catch {
      return null;
    }
  }
  function listSessions(projectRoot2) {
    const sessions = [];
    for (const path of listSessionFiles(projectRoot2)) {
      try {
        const session = JSON.parse(CS.System.IO.File.ReadAllText(path));
        if (isSessionEmpty(session)) {
          continue;
        }
        sessions.push({
          id: session.id,
          title: session.title,
          createdAt: session.createdAt,
          updatedAt: session.updatedAt,
          parentSessionId: session.parentSessionId,
          provider: session.provider,
          modelId: session.modelId,
          messageCount: session.messageCount
        });
      } catch {
      }
    }
    sessions.sort((a, b) => String(b.updatedAt).localeCompare(String(a.updatedAt)));
    return sessions;
  }
  function formatSessionTree(projectRoot2) {
    const sessions = listSessions(projectRoot2);
    if (sessions.length === 0) return "No saved sessions.";
    const children = /* @__PURE__ */ new Map();
    const roots = [];
    for (const session of sessions) {
      if (!session.parentSessionId) {
        roots.push(session);
        continue;
      }
      const bucket = children.get(session.parentSessionId) || [];
      bucket.push(session);
      children.set(session.parentSessionId, bucket);
    }
    const lines = [];
    function render(node, depth) {
      const indent = "  ".repeat(depth);
      lines.push(`${indent}- ${node.id} | ${node.title} | ${node.modelId} | ${node.updatedAt}`);
      const childSessions = children.get(node.id) || [];
      childSessions.sort((a, b) => String(b.updatedAt).localeCompare(String(a.updatedAt)));
      for (const child of childSessions) render(child, depth + 1);
    }
    roots.sort((a, b) => String(b.updatedAt).localeCompare(String(a.updatedAt)));
    for (const root of roots) render(root, 0);
    return lines.join("\n");
  }

  // src/extensions/registry.ts
  var extensionTools = [];
  var beforeToolCallHooks = [];
  var afterToolCallHooks = [];
  var onErrorHooks = [];
  var systemPromptPatches = [];
  var loadedExtensions = [];
  var typeBoxKind = Symbol.for("TypeBox.Kind");
  function isTypeBoxSchema(value) {
    return !!value && typeof value === "object" && typeBoxKind in value;
  }
  function convertJsonSchemaToTypeBox(schema) {
    if (!schema || typeof schema !== "object") {
      throw new Error("Tool parameters must be a TypeBox schema or a JSON schema object.");
    }
    if (isTypeBoxSchema(schema)) {
      return schema;
    }
    const description = typeof schema.description === "string" ? schema.description : void 0;
    const options = description ? { description } : void 0;
    switch (schema.type) {
      case "string":
        return Type.String(options);
      case "number":
        return Type.Number(options);
      case "integer":
        return Type.Integer(options);
      case "boolean":
        return Type.Boolean(options);
      case "null":
        return Type.Null(options);
      case "array":
        return Type.Array(convertJsonSchemaToTypeBox(schema.items ?? { type: "string" }), options);
      case "object": {
        const properties = schema.properties ?? {};
        const required = new Set(Array.isArray(schema.required) ? schema.required : []);
        const converted = {};
        for (const [key, value] of Object.entries(properties)) {
          const child = convertJsonSchemaToTypeBox(value);
          converted[key] = required.has(key) ? child : Type.Optional(child);
        }
        return Type.Object(converted, options);
      }
      default:
        throw new Error(`Unsupported JSON schema type for extension tool: ${String(schema.type ?? "unknown")}`);
    }
  }
  function extensionLog(context, message) {
    context.log(`[extension] ${message}`);
  }
  function tryParseHostResult(raw) {
    if (typeof raw !== "string") {
      return raw;
    }
    const trimmed = raw.trim();
    if (!trimmed) return "";
    const looksLikeJson = trimmed.startsWith("{") && trimmed.endsWith("}") || trimmed.startsWith("[") && trimmed.endsWith("]") || trimmed === "null" || trimmed === "true" || trimmed === "false" || /^-?\d+(\.\d+)?$/.test(trimmed);
    if (!looksLikeJson) {
      return raw;
    }
    try {
      return JSON.parse(trimmed);
    } catch {
      return raw;
    }
  }
  function resetExtensionRegistry() {
    extensionTools.length = 0;
    beforeToolCallHooks.length = 0;
    afterToolCallHooks.length = 0;
    onErrorHooks.length = 0;
    systemPromptPatches.length = 0;
    loadedExtensions.length = 0;
  }
  function getExtensionTools() {
    return extensionTools.slice();
  }
  function getLoadedExtensions() {
    return loadedExtensions.slice();
  }
  function getSystemPromptPatches() {
    return systemPromptPatches.slice();
  }
  function buildRegisteredHooks() {
    if (beforeToolCallHooks.length === 0 && afterToolCallHooks.length === 0 && onErrorHooks.length === 0) {
      return void 0;
    }
    return {
      beforeToolCall: beforeToolCallHooks.length === 0 ? void 0 : async (toolName, args) => {
        for (const hook of beforeToolCallHooks) {
          const result = await hook(toolName, args);
          if (result?.block) {
            return result;
          }
        }
        return void 0;
      },
      afterToolCall: afterToolCallHooks.length === 0 ? void 0 : async (toolName, args, result, isError) => {
        for (const hook of afterToolCallHooks) {
          await hook(toolName, args, result, isError);
        }
      },
      onError: onErrorHooks.length === 0 ? void 0 : (error, ctx) => {
        for (const hook of onErrorHooks) {
          hook(error, ctx);
        }
      }
    };
  }
  function createExtensionApi(context) {
    return {
      ...context,
      registerTool(tool) {
        const normalizedTool = {
          ...tool,
          parameters: convertJsonSchemaToTypeBox(tool.parameters)
        };
        extensionTools.push(normalizedTool);
        extensionLog(context, `registered tool: ${tool.name}`);
      },
      registerBeforeToolCall(hook) {
        beforeToolCallHooks.push(hook);
        extensionLog(context, "registered beforeToolCall hook");
      },
      registerAfterToolCall(hook) {
        afterToolCallHooks.push(hook);
        extensionLog(context, "registered afterToolCall hook");
      },
      registerOnError(hook) {
        onErrorHooks.push(hook);
        extensionLog(context, "registered onError hook");
      },
      appendSystemPrompt(text) {
        const trimmed = (text ?? "").trim();
        if (!trimmed) return;
        systemPromptPatches.push(trimmed);
        extensionLog(context, "appended system prompt patch");
      },
      async callHost(name, args) {
        const json = JSON.stringify(args ?? {});
        const raw = globalThis.pieBridge?.callHost?.(name, json);
        return tryParseHostResult(raw);
      }
    };
  }
  function registerLoadedExtension(path) {
    loadedExtensions.push(path);
  }

  // src/extensions/load-project-extensions.ts
  function getSystemIO2() {
    if (typeof CS === "undefined" || !CS?.System?.IO) {
      throw new Error("CS.System.IO not available - not running in PuerTS environment");
    }
    return CS.System.IO;
  }
  function normalizePath2(path) {
    return path.replace(/\\/g, "/");
  }
  function readExtensionFiles(extensionRoots) {
    const IO = getSystemIO2();
    const byName = /* @__PURE__ */ new Map();
    for (const extensionsRoot of extensionRoots) {
      if (!IO.Directory.Exists(extensionsRoot)) {
        continue;
      }
      const files = IO.Directory.GetFiles(extensionsRoot, "*.js", IO.SearchOption.AllDirectories);
      for (let i = 0; i < files.Length; i++) {
        const filePath = normalizePath2(String(files.get_Item(i)));
        const fileName = String(CS.System.IO.Path.GetFileNameWithoutExtension(filePath) ?? filePath).toLowerCase();
        byName.delete(fileName);
        byName.set(fileName, filePath);
      }
    }
    return Array.from(byName.values());
  }
  function loadProjectExtensions(context) {
    resetExtensionRegistry();
    const IO = getSystemIO2();
    const extensionRoots = getExtensionSearchPaths(context.projectRoot);
    for (const root of extensionRoots) {
      if (!IO.Directory.Exists(root)) {
        context.log(`[extension] search path not found: ${root}`);
      }
    }
    const files = readExtensionFiles(extensionRoots);
    const loaded = [];
    const errors = [];
    for (const filePath of files) {
      try {
        const code = String(IO.File.ReadAllText(filePath) ?? "");
        const api = createExtensionApi(context);
        const sourceUrl = normalizePath2(filePath);
        const factory = new Function(
          "pieExtension",
          `"use strict";
${code}
//# sourceURL=${sourceUrl}`
        );
        let activated = false;
        const pieExtension = (activate) => {
          if (typeof activate !== "function") {
            throw new Error("Extension must call pieExtension((api) => { ... })");
          }
          activated = true;
          const result = activate(api);
          if (result && typeof result.then === "function") {
            throw new Error("Async extension activation is not supported in the initial loader.");
          }
        };
        factory(pieExtension);
        if (!activated) {
          throw new Error("Extension did not call pieExtension((api) => { ... })");
        }
        registerLoadedExtension(sourceUrl);
        loaded.push(sourceUrl);
        context.log(`[extension] loaded ${sourceUrl}`);
      } catch (error) {
        const message = `[extension] failed ${filePath}: ${error?.message || String(error)}`;
        errors.push(message);
        context.log(message);
      }
    }
    return { loaded, errors };
  }

  // src/tools/read-skill.ts
  function createReadSkillTool(projectRoot2) {
    return {
      name: "read_skill",
      label: "read_skill",
      description: "Load the full contents of a builtin or project skill by name.",
      parameters: Type.Object({
        name: Type.String({ description: "Exact skill name from the available_skills list." })
      }),
      execute: async (_toolCallId, params) => {
        const skill = readSkillContent(projectRoot2, params.name);
        if (!skill?.content) {
          return {
            content: [{ type: "text", text: `Skill not found: ${params.name}` }],
            details: { found: false, name: params.name },
            isError: true
          };
        }
        return {
          content: [{
            type: "text",
            text: `# ${skill.name}
source: ${skill.source}

${skill.content}`
          }],
          details: { found: true, name: skill.name, source: skill.source }
        };
      }
    };
  }

  // src/index.ts
  var bridge = globalThis.pieBridge;
  if (!bridge) throw new Error("[pie] globalThis.pieBridge not found \u2014 was PieBridge.cs initialized?");
  var projectRoot = bridge.getProjectRoot();
  var isEditor = bridge.isEditor === true;
  var persistentDataPath = typeof bridge.getPersistentDataPath === "function" ? String(bridge.getPersistentDataPath() || "").replace(/\\/g, "/") : "";
  function parseJsonStringArray(raw) {
    if (!raw) return [];
    try {
      const parsed = JSON.parse(String(raw));
      return Array.isArray(parsed) ? parsed.filter((item) => typeof item === "string") : [];
    } catch {
      return [];
    }
  }
  function uniquePaths(paths) {
    const seen = /* @__PURE__ */ new Set();
    const results = [];
    for (const value of paths) {
      if (!value || typeof value !== "string") continue;
      const normalized = String(value).replace(/\\/g, "/");
      if (seen.has(normalized)) continue;
      seen.add(normalized);
      results.push(normalized);
    }
    return results;
  }
  function buildFileToolPathOptions() {
    const runtimeStateRoot = typeof bridge.getRuntimeStateRoot === "function" ? String(bridge.getRuntimeStateRoot() || "") : "";
    const sessionsDirectory = typeof bridge.getSessionsDirectory === "function" ? String(bridge.getSessionsDirectory() || "") : "";
    const extensionPaths = typeof bridge.getExtensionSearchPathsJson === "function" ? parseJsonStringArray(String(bridge.getExtensionSearchPathsJson() || "[]")) : [];
    const skillPaths = typeof bridge.getSkillSearchPathsJson === "function" ? parseJsonStringArray(String(bridge.getSkillSearchPathsJson() || "[]")) : [];
    const editorOnlyPaths = isEditor ? [projectRoot, ...extensionPaths, ...skillPaths] : [];
    return {
      allowlistedDirs: uniquePaths([
        persistentDataPath,
        runtimeStateRoot,
        sessionsDirectory,
        ...editorOnlyPaths
      ])
    };
  }
  var httpClient = new UnityHttpClient();
  setHttpClient(httpClient);
  var PROVIDER_PRESETS = {
    "kimi-cn": { baseUrl: "https://api.moonshot.cn/v1", api: "openai-completions" },
    "moonshot": { baseUrl: "https://api.moonshot.cn/v1", api: "openai-completions" },
    "openai": { baseUrl: "https://api.openai.com/v1", api: "openai-completions" },
    "anthropic": { baseUrl: "https://api.anthropic.com", api: "anthropic-messages" },
    "openrouter": { baseUrl: "https://openrouter.ai/api/v1", api: "openai-completions" },
    "bigmodel": { baseUrl: "https://open.bigmodel.cn/api/paas/v4", api: "openai-completions" },
    "deepseek": { baseUrl: "https://api.deepseek.com/v1", api: "openai-completions" },
    "ollama": { baseUrl: "http://localhost:11434/v1", api: "openai-completions" }
  };
  var MODEL_PRESETS = {
    "openai-fast": { provider: "openai", modelId: "gpt-4.1-mini", label: "GPT-4.1 mini" },
    "openai-strong": { provider: "openai", modelId: "gpt-4.1", label: "GPT-4.1" },
    "kimi-fast": { provider: "kimi-cn", modelId: "kimi-k2.5", label: "Kimi K2.5" },
    "claude-fast": { provider: "anthropic", modelId: "claude-3-5-haiku-latest", label: "Claude 3.5 Haiku" },
    "claude-strong": { provider: "anthropic", modelId: "claude-3-7-sonnet-latest", label: "Claude Sonnet" }
  };
  function getBaseConfig(provider) {
    const preset = PROVIDER_PRESETS[provider.toLowerCase()];
    if (preset) return preset;
    for (const [key, val] of Object.entries(PROVIDER_PRESETS)) {
      if (provider.toLowerCase().startsWith(key)) return val;
    }
    return { baseUrl: `https://api.${provider}.com/v1`, api: "openai-completions" };
  }
  function buildSystemPrompt(root, editorMode) {
    const skillsSection = formatSkillsForPrompt(getAvailableSkills(root));
    const projectMemory = getProjectMemoryExcerpt(root);
    const extensionSection = getSystemPromptPatches();
    const memorySection = projectMemory ? `

[Project Memory]
${projectMemory}
---` : "";
    const extensionsPromptSection = extensionSection.length > 0 ? `

[Project Extensions]
${extensionSection.join("\n\n---\n\n")}
---` : "";
    if (editorMode) {
      return [
        "You are Pie, an expert AI coding assistant embedded in Unity Editor.",
        `The Unity project is located at: ${root}`,
        `Default filesystem root is Application.persistentDataPath: ${persistentDataPath || "(unavailable)"}`,
        "You can work with files using these structured tools: read_file, write_file, edit_file, list_directory, search_file_content, search_files.",
        "In Editor, both Application.persistentDataPath and project paths such as Assets/... are accessible.",
        "You can execute JavaScript in Unity via the eval_js tool and the global CS object.",
        "Prefer making small, targeted edits. Always read a file before writing it.",
        "Be concise and helpful.",
        skillsSection,
        memorySection,
        extensionsPromptSection
      ].join("\n");
    }
    return [
      "You are Pie, an AI agent running inside a Unity runtime application.",
      `Your primary working directory is Application.persistentDataPath: ${persistentDataPath || root}`,
      "You can work with files using these structured tools: read_file, write_file, edit_file, list_directory, search_file_content, search_files.",
      "At runtime, treat Application.persistentDataPath as the default filesystem root. Do not assume Assets/... paths are accessible.",
      "You can execute JavaScript in Unity via the eval_js tool and the global CS object.",
      "You are intended for runtime automation, NPC behaviors, and voice-to-action control.",
      "Be concise and helpful.",
      skillsSection,
      memorySection,
      extensionsPromptSection
    ].join("\n");
  }
  function buildModel(provider, modelId, baseUrlOverride = "") {
    const base = getBaseConfig(provider);
    const effectiveBaseUrl = baseUrlOverride.trim().length > 0 ? baseUrlOverride.trim() : base.baseUrl;
    return {
      id: modelId,
      name: modelId,
      api: base.api,
      provider,
      baseUrl: effectiveBaseUrl,
      reasoning: false,
      input: ["text"],
      cost: { input: 0, output: 0, cacheRead: 0, cacheWrite: 0 },
      contextWindow: 128e3,
      maxTokens: 8192
    };
  }
  function aliasTool(tool, name, label, description) {
    return {
      ...tool,
      name,
      label,
      description: description ?? tool.description
    };
  }
  var _apiKey = "";
  var _provider = "openai";
  var _modelId = "gpt-4.1-mini";
  var _baseUrl = "";
  var _verboseLogs = false;
  var _currentSession = createSessionRecord({
    provider: _provider,
    modelId: _modelId,
    messages: [],
    todoState: []
  });
  var todoTool = createManageTodoListTool();
  var fileToolPathOptions = buildFileToolPathOptions();
  var fileToolRoot = persistentDataPath || projectRoot;
  var builtinFileTools = [
    aliasTool(
      createReadTool(fileToolRoot, fileToolPathOptions),
      "read_file",
      "read_file",
      "Read a file by relative path. Default root is Application.persistentDataPath. In Editor, project paths such as Assets/... are also allowed."
    ),
    aliasTool(
      createWriteTool(fileToolRoot, fileToolPathOptions),
      "write_file",
      "write_file",
      "Write a file by relative path. Default root is Application.persistentDataPath. In Editor, project paths are also allowed."
    ),
    aliasTool(
      createEditTool(fileToolRoot, fileToolPathOptions),
      "edit_file",
      "edit_file",
      "Edit an existing file by replacing exact oldText with newText. Read the file first so oldText matches exactly."
    ),
    aliasTool(
      createLsTool(fileToolRoot, fileToolPathOptions),
      "list_directory",
      "list_directory",
      "List a directory by relative path. Default root is Application.persistentDataPath. In Editor, project paths are also allowed."
    ),
    aliasTool(
      createNativeGrepTool(fileToolRoot, fileToolPathOptions),
      "search_file_content",
      "search_file_content",
      "Search inside files for text or regex. Best practice: pattern='MonoBehaviour', glob='*.cs', literal=true."
    ),
    aliasTool(
      createNativeFindTool(fileToolRoot, fileToolPathOptions),
      "search_files",
      "search_files",
      "Search for files by filename fragment or glob pattern. Best practice: use a simple filename like 'DemoScript' or 'DemoScript.cs'. Do not pass a shell command."
    )
  ];
  function createBuiltinTools() {
    return [
      ...builtinFileTools,
      createEvalJsTool(),
      todoTool.tool,
      createReadSkillTool(projectRoot)
    ];
  }
  function rebuildAgentSurface() {
    const tools = [
      ...createBuiltinTools(),
      ...getExtensionTools()
    ];
    agent.setTools(tools);
    const hooks = buildRegisteredHooks();
    const setHooks = agent.setHooks;
    if (typeof setHooks === "function") {
      setHooks.call(agent, hooks);
    } else if (hooks) {
      bridge.log("warn", "[pie/extensions] Agent hooks are not available in this build; registered hooks will be ignored.");
    }
    agent.setSystemPrompt(buildSystemPrompt(projectRoot, isEditor));
  }
  var agent = new Agent({
    initialState: {
      systemPrompt: buildSystemPrompt(projectRoot, isEditor),
      model: buildModel(_provider, _modelId, _baseUrl),
      tools: createBuiltinTools()
    },
    sessionId: _currentSession.id,
    getApiKey: () => _apiKey || void 0
  });
  function syncSessionInfo() {
    agent.sessionId = _currentSession.id;
  }
  function verboseLog(message) {
    if (_verboseLogs) {
      bridge.log("info", message);
    }
  }
  function reloadProjectExtensions() {
    const result = loadProjectExtensions({
      projectRoot,
      isEditor,
      log: verboseLog
    });
    rebuildAgentSurface();
    return result;
  }
  function persistCurrentSession() {
    const nextRecord = {
      ..._currentSession,
      provider: _provider,
      modelId: _modelId,
      messages: agent.state.messages.slice(),
      todoState: todoTool.getState()
    };
    if (isSessionEmpty(nextRecord)) {
      deleteSession(projectRoot, nextRecord.id);
      _currentSession = {
        ...nextRecord,
        title: "New Session",
        messageCount: 0
      };
      syncSessionInfo();
      return;
    }
    _currentSession = saveSession(projectRoot, nextRecord);
    syncSessionInfo();
  }
  function cancelActivePromptForSessionSwitch() {
    agent.abort();
    agent.resetProcessingState();
  }
  function startFreshSession(parentSessionId) {
    cancelActivePromptForSessionSwitch();
    _currentSession = createSessionRecord({
      parentSessionId,
      provider: _provider,
      modelId: _modelId,
      messages: [],
      todoState: []
    });
    agent.reset();
    agent.setModel(buildModel(_provider, _modelId, _baseUrl));
    rebuildAgentSurface();
    todoTool.reset();
    syncSessionInfo();
    emitSessionSync();
  }
  function restoreSessionRecord(record) {
    cancelActivePromptForSessionSwitch();
    _currentSession = record;
    _provider = record.provider || _provider;
    _modelId = record.modelId || _modelId;
    agent.replaceMessages(record.messages || []);
    agent.setModel(buildModel(_provider, _modelId, _baseUrl));
    rebuildAgentSurface();
    todoTool.restore(record.todoState || []);
    syncSessionInfo();
    emitSessionSync();
  }
  function forkFromRecord(record) {
    const forked = createSessionRecord({
      parentSessionId: record.id,
      provider: record.provider || _provider,
      modelId: record.modelId || _modelId,
      messages: (record.messages || []).slice(),
      todoState: (record.todoState || []).slice()
    });
    restoreSessionRecord(forked);
    persistCurrentSession();
  }
  function emitLocalAssistantMessage(text) {
    bridge.sendToUnity("message_update", { type: "text_full", text });
    bridge.sendToUnity("turn_end", { role: "assistant", stopReason: "stop" });
    bridge.sendToUnity("agent_end", {});
  }
  function hasRealUsage(usage) {
    if (!usage || typeof usage !== "object") return false;
    return Number(usage.input || 0) > 0 || Number(usage.output || 0) > 0 || Number(usage.cacheRead || 0) > 0 || Number(usage.cacheWrite || 0) > 0 || Number(usage.totalTokens || 0) > 0;
  }
  function estimateTokensFromText(text) {
    const normalized = (text || "").trim();
    if (!normalized) return 0;
    return Math.max(1, Math.ceil(normalized.length / 4));
  }
  function collectMessageText(message) {
    if (!message) return "";
    const content = message.content;
    if (typeof content === "string") return content;
    if (!Array.isArray(content)) return "";
    return content.map((block) => {
      if (!block || typeof block !== "object") return "";
      if (block.type === "text") return typeof block.text === "string" ? block.text : "";
      if (block.type === "thinking") return typeof block.thinking === "string" ? block.thinking : "";
      if (block.type === "toolCall") {
        const name = typeof block.name === "string" ? block.name : "";
        const args = block.arguments !== void 0 ? JSON.stringify(block.arguments) : "";
        return `${name}${args}`;
      }
      return "";
    }).join("");
  }
  function buildUsagePayload(messages, message, fallbackIndex) {
    if (hasRealUsage(message?.usage)) {
      return {
        input: Number(message.usage.input || 0),
        output: Number(message.usage.output || 0),
        cacheRead: Number(message.usage.cacheRead || 0),
        cacheWrite: Number(message.usage.cacheWrite || 0),
        totalTokens: Number(message.usage.totalTokens || 0),
        estimated: false
      };
    }
    if ((message?.role || "") !== "assistant") {
      return null;
    }
    const resolvedIndex = typeof fallbackIndex === "number" ? fallbackIndex : Math.max(0, messages.lastIndexOf(message));
    let inputTokens = 0;
    for (let i = resolvedIndex - 1; i >= 0; i--) {
      const candidate = messages[i];
      if ((candidate?.role || "") !== "user") continue;
      inputTokens = estimateTokensFromText(collectMessageText(candidate));
      if (inputTokens > 0) break;
    }
    const outputTokens = estimateTokensFromText(collectMessageText(message));
    const totalTokens = inputTokens + outputTokens;
    if (totalTokens <= 0) return null;
    return {
      input: inputTokens,
      output: outputTokens,
      cacheRead: 0,
      cacheWrite: 0,
      totalTokens,
      estimated: true
    };
  }
  function emitSessionSync() {
    const stateMessages = agent.state.messages || [];
    const messages = stateMessages.map((message, index) => ({
      role: String(message?.role || "assistant"),
      content: Array.isArray(message?.content) ? message.content.filter((block) => block && typeof block === "object").map((block) => ({
        type: String(block?.type || "text"),
        text: typeof block?.text === "string" ? block.text : ""
      })) : [],
      errorMessage: typeof message?.errorMessage === "string" ? message.errorMessage : "",
      stopReason: typeof message?.stopReason === "string" ? message.stopReason : "",
      usage: buildUsagePayload(stateMessages, message, index)
    }));
    bridge.sendToUnity("session_sync", {
      id: _currentSession.id,
      title: _currentSession.title,
      messageCount: messages.length,
      messages
    });
  }
  function emitSkillsList() {
    const skills = getAvailableSkills(projectRoot).map((skill) => ({
      name: skill.name,
      description: skill.description,
      source: skill.source,
      path: skill.path || ""
    }));
    bridge.sendToUnity("skills_list", { skills });
  }
  function applyModelSelection(provider, modelId) {
    _provider = provider;
    _modelId = modelId;
    agent.setModel(buildModel(_provider, _modelId, _baseUrl));
    rebuildAgentSurface();
    persistCurrentSession();
  }
  function extractToolText(result) {
    if (!result) return "";
    if (typeof result === "string") return result;
    if (Array.isArray(result.content)) {
      return result.content.filter((item) => item?.type === "text").map((item) => String(item.text ?? "")).join("\n").trim();
    }
    return "";
  }
  function extractEffectiveArgs(toolName, args, details) {
    if (toolName === "search_files") {
      return {
        rawPattern: details?.rawPattern ?? args?.pattern ?? args?.query ?? args?.name ?? args?.file_name ?? "",
        pattern: details?.effectivePattern ?? args?.pattern ?? args?.query ?? args?.name ?? args?.file_name ?? "",
        path: details?.searchPath ?? args?.path ?? ".",
        limit: args?.limit
      };
    }
    if (toolName === "search_file_content") {
      return {
        pattern: details?.effectivePattern ?? args?.pattern ?? "",
        glob: details?.glob ?? args?.glob ?? "",
        path: details?.searchPath ?? args?.path ?? ".",
        literal: details?.literal ?? args?.literal,
        ignoreCase: details?.ignoreCase ?? args?.ignoreCase,
        limit: args?.limit
      };
    }
    return args ?? {};
  }
  function tryExplainExternalSessionId(sessionId) {
    const trimmed = String(sessionId || "").trim();
    if (!trimmed) return null;
    const registryPath = CS.System.IO.Path.Combine(
      CS.System.Environment.GetFolderPath(CS.System.Environment.SpecialFolder.UserProfile),
      ".unity_skills",
      "registry.json"
    );
    if (!CS.System.IO.File.Exists(registryPath)) {
      return null;
    }
    try {
      const registry = JSON.parse(String(CS.System.IO.File.ReadAllText(registryPath) || "{}"));
      const entries = Object.values(registry || {});
      const matched = entries.find((entry) => String(entry?.id || "") === trimmed);
      if (!matched) {
        return null;
      }
      const availableSessions = listSessions(projectRoot).slice(0, 8).map((session) => `- ${session.id} | ${session.title} | ${session.updatedAt}`);
      const availableText = availableSessions.length > 0 ? `

Available Pie sessions:
${availableSessions.join("\n")}` : "\n\nNo Pie chat sessions have been saved yet.";
      return [
        `\`${trimmed}\` is a UnitySkills instance ID for project \`${matched.name || matched.path || "unknown"}\`, not a pie-unity chat session ID.`,
        "pie-unity chat sessions use IDs like `sess_xxx` and are stored separately under Application.persistentDataPath/Pie/sessions.",
        "Use `/resume` with one of the Pie session IDs shown in the Sessions panel or returned by `/resume` with no arguments.",
        availableText
      ].join("\n");
    } catch {
      return null;
    }
  }
  function handleLocalCommand(content) {
    const trimmed = content.trim();
    if (!trimmed.startsWith("/")) return false;
    const [command, ...rest] = trimmed.split(/\s+/);
    if (command === "/new") {
      persistCurrentSession();
      startFreshSession();
      emitLocalAssistantMessage(`Started new session: ${_currentSession.id}`);
      return true;
    }
    if (command === "/resume") {
      const sessionId = rest[0];
      if (!sessionId) {
        const sessions = listSessions(projectRoot);
        const text = sessions.length === 0 ? "No saved sessions." : sessions.map((session) => `${session.id} | ${session.title} | ${session.modelId} | ${session.updatedAt}`).join("\n");
        emitLocalAssistantMessage(text);
        return true;
      }
      const loaded = loadSession(projectRoot, sessionId);
      if (!loaded) {
        const explanation = tryExplainExternalSessionId(sessionId);
        emitLocalAssistantMessage(explanation || `Session not found: ${sessionId}`);
        return true;
      }
      restoreSessionRecord(loaded);
      emitLocalAssistantMessage(`Resumed session ${loaded.id}
Title: ${loaded.title}
Messages: ${loaded.messageCount}`);
      return true;
    }
    if (command === "/tree") {
      emitLocalAssistantMessage(formatSessionTree(projectRoot));
      return true;
    }
    if (command === "/session-check") {
      const probe = createSessionRecord({
        sessionId: `probe_${Date.now().toString(36)}`,
        provider: _provider,
        modelId: _modelId,
        messages: agent.state.messages.slice(),
        todoState: todoTool.getState()
      });
      const saved = saveSession(projectRoot, probe);
      const loaded = loadSession(projectRoot, saved.id);
      const ok = !!loaded && loaded.id === saved.id && loaded.provider === saved.provider && loaded.modelId === saved.modelId && (loaded.messages?.length || 0) === saved.messages.length && (loaded.todoState?.length || 0) === saved.todoState.length;
      emitLocalAssistantMessage(
        ok ? `Session self-check passed.
Session: ${saved.id}
Messages: ${saved.messages.length}
Todos: ${saved.todoState.length}` : `Session self-check failed.
Saved: ${saved.id}
Loaded: ${loaded ? loaded.id : "(missing)"}`
      );
      return true;
    }
    if (command === "/fork") {
      const sessionId = rest[0];
      if (sessionId) {
        const source2 = loadSession(projectRoot, sessionId);
        if (!source2) {
          const explanation = tryExplainExternalSessionId(sessionId);
          emitLocalAssistantMessage(explanation || `Session not found: ${sessionId}`);
          return true;
        }
        forkFromRecord(source2);
        emitLocalAssistantMessage(`Forked new session ${_currentSession.id} from ${source2.id}`);
        return true;
      }
      persistCurrentSession();
      const source = loadSession(projectRoot, _currentSession.id) || _currentSession;
      forkFromRecord(source);
      emitLocalAssistantMessage(`Forked new session ${_currentSession.id} from ${source.id}`);
      return true;
    }
    if (command === "/memory") {
      const memory = readProjectMemory(projectRoot);
      emitLocalAssistantMessage(
        memory ? `Project memory loaded from Assets/Pie/AGENTS.md

${memory}` : "No project memory found. Use /init to create an initial AGENTS.md."
      );
      return true;
    }
    if (command === "/model") {
      if (rest.length === 0) {
        const presets = Object.entries(MODEL_PRESETS).map(([key, value]) => `${key} => ${value.provider}/${value.modelId} (${value.label})`).join("\n");
        emitLocalAssistantMessage(`Current model: ${_provider}/${_modelId}

Presets:
${presets}`);
        return true;
      }
      if (rest.length === 1) {
        const preset = MODEL_PRESETS[rest[0]];
        if (!preset) {
          emitLocalAssistantMessage(`Unknown model preset: ${rest[0]}`);
          return true;
        }
        applyModelSelection(preset.provider, preset.modelId);
        emitLocalAssistantMessage(`Switched model to ${preset.provider}/${preset.modelId}`);
        return true;
      }
      const provider = rest[0];
      const modelId = rest.slice(1).join(" ");
      applyModelSelection(provider, modelId);
      emitLocalAssistantMessage(`Switched model to ${provider}/${modelId}`);
      return true;
    }
    if (command === "/skills") {
      const subcommand = rest[0] || "list";
      if (subcommand === "reload") {
        rebuildAgentSurface();
        const skills2 = getAvailableSkills(projectRoot);
        emitSkillsList();
        emitLocalAssistantMessage(`Reloaded ${skills2.length} skill(s).`);
        return true;
      }
      const skills = getAvailableSkills(projectRoot);
      emitSkillsList();
      if (skills.length === 0) {
        emitLocalAssistantMessage("No skills available.");
        return true;
      }
      emitLocalAssistantMessage(
        skills.map((skill) => `${skill.name} [${skill.source}] - ${skill.description}`).join("\n")
      );
      return true;
    }
    if (command === "/extensions") {
      const subcommand = rest[0] || "list";
      if (subcommand === "reload") {
        const result = reloadProjectExtensions();
        const loaded2 = getLoadedExtensions();
        const loadedText = loaded2.length > 0 ? loaded2.join("\n") : "(none)";
        const errorText = result.errors.length > 0 ? `

Errors:
${result.errors.join("\n")}` : "";
        emitLocalAssistantMessage(`Reloaded ${loaded2.length} extension(s).

${loadedText}${errorText}`);
        return true;
      }
      const loaded = getLoadedExtensions();
      emitLocalAssistantMessage(
        loaded.length > 0 ? `Loaded extensions:
${loaded.join("\n")}` : "No project extensions loaded. Add .js files under Assets/Pie/Extensions or configure additional search paths in Pie Settings."
      );
      return true;
    }
    if (command === "/init") {
      const snapshot = getUnityContextSnapshot(projectRoot, isEditor);
      const initialMemory = buildInitialProjectMemory(snapshot);
      const writeTool = createWriteProjectMemoryTool(projectRoot);
      writeTool.execute("local-init", { content: initialMemory });
      agent.setSystemPrompt(buildSystemPrompt(projectRoot, isEditor));
      emitLocalAssistantMessage(`Initialized project memory at Assets/Pie/AGENTS.md

${initialMemory}`);
      return true;
    }
    return false;
  }
  reloadProjectExtensions();
  emitSessionSync();
  agent.subscribe((event) => {
    try {
      switch (event.type) {
        case "agent_start":
          bridge.sendToUnity("state_event", { name: "agent_start", detail: "agent loop started" });
          break;
        case "turn_start":
          bridge.sendToUnity("state_event", { name: "turn_start", detail: "turn started" });
          break;
        case "message_start":
          bridge.sendToUnity("state_event", { name: "message_start", detail: `role=${event.message?.role ?? "assistant"}` });
          break;
        case "message_update": {
          const msg = event.message;
          const text = Array.isArray(msg.content) ? msg.content.filter((c) => c?.type === "text").map((c) => c.text).join("") : "";
          if (event.assistantMessageEvent?.type === "thinking_start") {
            bridge.sendToUnity("thinking_event", { type: "start" });
          } else if (event.assistantMessageEvent?.type === "thinking_delta") {
            bridge.sendToUnity("thinking_event", {
              type: "delta",
              delta: event.assistantMessageEvent.delta ?? ""
            });
          } else if (event.assistantMessageEvent?.type === "thinking_end") {
            bridge.sendToUnity("thinking_event", {
              type: "end",
              text: event.assistantMessageEvent.content ?? ""
            });
          }
          bridge.sendToUnity("state_event", {
            name: "message_update",
            detail: `type=${event.assistantMessageEvent?.type ?? "text"} chars=${text.length}`
          });
          if (text) {
            bridge.sendToUnity(
              "message_update",
              { type: "text_full", text }
            );
          }
          break;
        }
        case "message_end":
          if (event.message?.role === "assistant") {
            const usage = buildUsagePayload(agent.state.messages || [], event.message);
            bridge.sendToUnity("message_metrics", {
              role: "assistant",
              usage
            });
          }
          bridge.sendToUnity("state_event", { name: "message_end", detail: `role=${event.message?.role ?? "assistant"}` });
          break;
        case "tool_execution_start":
          verboseLog(`[tool_start payload] ${JSON.stringify({
            name: event.toolName,
            callId: event.toolCallId,
            argsJson: JSON.stringify(event.args ?? {}).substring(0, 4e3)
          })}`);
          bridge.sendToUnity(
            "tool_start",
            {
              name: event.toolName,
              callId: event.toolCallId,
              argsJson: JSON.stringify(event.args ?? {}).substring(0, 4e3)
            }
          );
          break;
        case "tool_execution_end":
          {
            const effectiveArgs = extractEffectiveArgs(event.toolName, event.args, event.result?.details);
            const resultText = extractToolText(event.result).substring(0, 4e3);
            const resultJson = JSON.stringify(event.result ?? "").substring(0, 4e3);
            const detailsJson = event.result?.details !== void 0 ? JSON.stringify(event.result.details).substring(0, 4e3) : "";
            verboseLog(`[tool_end payload] ${JSON.stringify({
              name: event.toolName,
              callId: event.toolCallId,
              argsJson: JSON.stringify(event.args ?? {}).substring(0, 4e3),
              effectiveArgsJson: JSON.stringify(effectiveArgs ?? {}).substring(0, 4e3),
              isError: !!event.isError
            })}`);
            bridge.sendToUnity("tool_end", {
              name: event.toolName,
              callId: event.toolCallId,
              argsJson: JSON.stringify(event.args ?? {}).substring(0, 4e3),
              effectiveArgsJson: JSON.stringify(effectiveArgs ?? {}).substring(0, 4e3),
              isError: !!event.isError,
              resultText,
              resultJson,
              detailsJson,
              error: event.error ? String(event.error?.message ?? event.error).substring(0, 4e3) : event.isError ? resultText || "Tool returned an error result." : void 0
            });
          }
          break;
        case "turn_end":
          persistCurrentSession();
          bridge.sendToUnity(
            "turn_end",
            { role: event.message.role, stopReason: event.message?.stopReason ?? "stop" }
          );
          bridge.sendToUnity("state_event", {
            name: "turn_end",
            detail: `stopReason=${event.message?.stopReason ?? "stop"}`
          });
          if (event.message?.stopReason === "error" || event.message?.stopReason === "aborted") {
            const errMsg = event.message?.errorMessage ?? "Request failed";
            bridge.sendToUnity("error", { message: errMsg });
          }
          break;
        case "agent_end":
          bridge.sendToUnity("state_event", { name: "agent_end", detail: "agent loop finished" });
          bridge.sendToUnity("agent_end", {});
          break;
      }
    } catch (err) {
      bridge.log("error", `[pie/index] Event forwarding error: ${err?.message || err}`);
    }
  });
  globalThis.pie = {
    /**
     * Entry point called by PieBridge.cs via jsEnv.Eval("pie.handleCSharpMessage(action, data)").
     * data is a plain JS object (already parsed from JSON by JsEnv eval).
     */
    handleCSharpMessage: async function(action, data) {
      try {
        switch (action) {
          case "send_message": {
            const content = data && typeof data.content === "string" ? data.content : String(data);
            if (handleLocalCommand(content)) {
              break;
            }
            agent.prompt(content).catch((err) => {
              const msg = err?.message || String(err);
              bridge.log("error", `[pie/index] agent.prompt rejected: ${msg}`);
              bridge.sendToUnity("error", { message: msg });
            });
            break;
          }
          case "set_config": {
            if (data?.apiKey !== void 0) _apiKey = data.apiKey;
            if (data?.provider !== void 0) _provider = data.provider;
            if (data?.model !== void 0) _modelId = data.model;
            if (data?.baseUrl !== void 0) _baseUrl = data.baseUrl;
            if (data?.verboseLogs !== void 0) _verboseLogs = data.verboseLogs === true;
            globalThis.__pieVerboseLogs = _verboseLogs;
            applyModelSelection(_provider, _modelId);
            break;
          }
          case "new_session": {
            persistCurrentSession();
            startFreshSession();
            break;
          }
          case "list_skills": {
            emitSkillsList();
            break;
          }
          case "reload_skills": {
            rebuildAgentSurface();
            emitSkillsList();
            break;
          }
          case "abort": {
            agent.abort();
            break;
          }
          default:
            bridge.log("warn", `[pie/index] Unknown action: ${action}`);
        }
      } catch (err) {
        const msg = err?.message || String(err);
        bridge.log("error", `[pie/index] Error in ${action}: ${msg}`);
        bridge.sendToUnity("error", { message: msg });
      }
    }
  };
})();
//# sourceMappingURL=core.js.map
