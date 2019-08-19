(*
   Copyright 2018 EkonBenefits

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*)

namespace FSharp.Data.FlatFileMeta

open System
open FSharp.Interop.Compose.System
open System.Runtime.CompilerServices

[<RequireQualifiedAccess>]
module Format =

    type FormatPairs<'T> = (string -> 'T) * (int -> 'T -> string)

    module Valid =
        let checkFinal length (value:string) =
            if length <> value.Length then
                invalidOp (sprintf "'%s' is '%i' long, which is longer than field of '%i'."
                                value value.Length length)
            value

    module Str =
        let fillToLengthWith char length =  Array.init length (fun _ -> char) |> String
        let fillToLength = fillToLengthWith ' '
        
    
        let getRightTrim = String.trimEnd [|' '|]
        let setRightPad length value  = 
                value
                    |> Option.ofObj
                    |> Option.defaultValue String.Empty
                    |> String.Full.padRight length ' '
                    |> Valid.checkFinal length
        let getLeftTrim = String.trimStart [|' '|]
        let setLeftPad length value =
                value
                    |> Option.ofObj
                    |> Option.defaultValue String.Empty
                    |> String.Full.padLeft length ' '  
                    |> Valid.checkFinal length
        
    module Int =
        let getReq (value:string) = value |> int
        let getOpt (value:string) = 
            value 
                |> Helper.optionOfStringWhitespace
                |> Option.map int
                |> Option.toNullable
         
        let setZerod length (value:int) =
            value 
                |> string 
                |> String.Full.padLeft length '0'
                |> Valid.checkFinal length
        
        let setOptZerod length (value: int Nullable) =
            match value |> Option.ofNullable with
                | Some (i) -> setZerod length i
                | None -> Str.fillToLength length
                
    module Int64 =
        let getReq (value:string) = value |> int64
        let getOpt (value:string) = 
            value 
                |> Helper.optionOfStringWhitespace
                |> Option.map int64
                |> Option.toNullable
         
        let setZerod length (value:int64) =
            value 
                |> string 
                |> String.Full.padLeft length '0'
                |> Valid.checkFinal length
        
        let setOptZerod length (value: int64 Nullable) =
            match value |> Option.ofNullable with
                | Some (i) -> setZerod length i
                | None -> Str.fillToLength length
                
    module Decimal =
        let toStringReq (decimalPlaces:int) (length:int) (value:decimal) =
            value * decimal(10.0 ** float(decimalPlaces))
                |> truncate 
                |> int 
                |> Int.setZerod length
        
        let parseReq (decimalPlaces:int) value =
            let intVal = Int.getReq value
            decimal(intVal) / decimal(10.0 ** float(decimalPlaces))

        let getReqMoney = parseReq 2
        let setReqMoney = toStringReq 2

    module DateAndTime =
        open System.Globalization
        let parseReq format value = DateTime.ParseExact(value, format, CultureInfo.InvariantCulture)
        
        let toStringReq format (length:int) (value:DateTime) = 
            value.ToString(format, CultureInfo.InvariantCulture) 
            |> Valid.checkFinal length
            
        let parseOpt (format:string) (value:string) = 
                    match DateTime.TryParseExact(value, 
                                                 format,
                                                 CultureInfo.InvariantCulture,
                                                 DateTimeStyles.NoCurrentDateDefault
                                                ) with
                        | true, d -> Some(d)
                        | _______ -> None
                    |> Option.toNullable
        
        let getOptJulianDate value =
            let intToJulian date =
                let jan1 = DateTime(DateTime.Today.Year, 1, 1)
                jan1 |> DateTime.addDays (float date)
            value |> Int.getOpt 
                  |> Option.ofNullable 
                  |> Option.map intToJulian 
                  |> Option.toNullable
        
        let setOptJulianDate (length:int) (value: DateTime Nullable) =
           let optValue = Option.ofNullable value
           match optValue with
                          | Some(d) -> d.DayOfYear |> int |> Int.setZerod length
                          | None -> length |> Str.fillToLength
        
        let toStringOpt format (length:int) (value:DateTime Nullable)=
           let optValue = Option.ofNullable value
           match optValue with
               | Some(d) -> d |> toStringReq format length
               | None -> length |> Str.fillToLength 
        
        let getYYMMDD = parseReq "yyMMdd"
        let setYYMMDD = toStringReq "yyMMdd"      
        
        let getOptYYMMDD = parseOpt "yyMMdd"
        let setOptYYMMDD = toStringOpt "yyMMdd"    
        
        let getOptHHMM = parseOpt "HHmm"
        let setOptHHMM = toStringOpt "HHmm"
     
    module Code =
        let getCode<'T when 'T :> DataCode<'T> and  'T: ( new : unit -> 'T )> value : 'T =
            DataCode<'T>.Create(value)
            
        let setCode<'T when 'T :> DataCode<'T> and  'T: ( new : unit -> 'T )> length (code:'T) =
            code.ToRawString()
            |> Valid.checkFinal length
        
    let zerodInt:FormatPairs<_>  = (Int.getReq, Int.setZerod)
    let zerodInt64:FormatPairs<_>  = (Int64.getReq, Int64.setZerod)
    
    let reqDataCode<'T when 'T :> DataCode<'T> and  'T: ( new : unit -> 'T )> : FormatPairs<'T> = (Code.getCode, Code.setCode)
    let reqMoney:FormatPairs<_> = (Decimal.getReqMoney, Decimal.setReqMoney)
    let rightPadString:FormatPairs<_> = (Str.getRightTrim, Str.setRightPad)
    let leftPadString:FormatPairs<_>  = (Str.getLeftTrim, Str.setLeftPad)
    let reqYYMMDD:FormatPairs<_>  = (DateAndTime.getYYMMDD, DateAndTime.setYYMMDD)
    let optYYMMDD:FormatPairs<_>  = (DateAndTime.getOptYYMMDD, DateAndTime.setOptYYMMDD)
    let optJulian:FormatPairs<_> = (DateAndTime.getOptJulianDate, DateAndTime.setOptJulianDate)
    let optHHMM:FormatPairs<_>  = (DateAndTime.getOptHHMM, DateAndTime.setOptHHMM)