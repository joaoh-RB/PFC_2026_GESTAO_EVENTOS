export function unmaskCnpj(value: string): string {
    return value.toUpperCase().replace(/[^A-Z0-9]/g, '').slice(0, 14);
}
export function maskCnpj(value: string): string {
    const clean = unmaskCnpj(value);
    let result = clean;
    if (clean.length > 2) {
        result = clean.slice(0, 2) + '.' + clean.slice(2);
    }
    if (clean.length > 5) {
        result = clean.slice(0, 2) + '.' + clean.slice(2, 5) + '.' + clean.slice(5);
    }
    if (clean.length > 8) {
        result = clean.slice(0, 2) + '.' + clean.slice(2, 5) + '.' + clean.slice(5, 8) + '/' + clean.slice(8);
    }
    if (clean.length > 12) {
        result = clean.slice(0, 2) + '.' + clean.slice(2, 5) + '.' + clean.slice(5, 8) + '/' + clean.slice(8, 12) + '-' + clean.slice(12);
    }

    return result;
}
export function unmaskPhone(value: string): string {
    return value.replace(/\D/g, '').slice(0, 11);
}
export function maskPhone(value: string): string {
    const clean = unmaskPhone(value);
    return clean
        .replace(/^(\d{2})(\d)/, '($1) $2')
        .replace(/(\d{5})(\d{4})$/, '$1-$2');
}
function calcDv(input: string, weights: number[]): number {
    let sum = 0;
    for (let i = 0; i < input.length; i++) {
        sum += (input.charCodeAt(i) - 48) * weights[i];
    }
    const remainder = sum % 11;
    return remainder < 2 ? 0 : 11 - remainder;
}
export function isValidCnpj(cnpj: string): boolean {
    const clean = unmaskCnpj(cnpj);
    if (!/^[A-Z0-9]{12}\d{2}$/.test(clean)) return false;
    const base = clean.slice(0, 12);
    const dv1Expected = Number(clean[12]);
    const dv2Expected = Number(clean[13]);
    const dv1 = calcDv(base, [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
    if (dv1 !== dv1Expected) return false;
    const dv2 = calcDv(base + dv1, [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
    return dv2 === dv2Expected;
}