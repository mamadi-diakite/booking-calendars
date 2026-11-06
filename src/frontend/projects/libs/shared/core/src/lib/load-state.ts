import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { catchError, map, Observable, of, startWith } from 'rxjs';

export type LoadState<T> =
  | { readonly status: 'loading' }
  | { readonly status: 'ready'; readonly value: T }
  | { readonly status: 'missing' }
  | { readonly status: 'error' };

/** Turns a one-shot request into loading, then ready, missing (404) or error. */
export function withLoadState<T>(source: Observable<T>): Observable<LoadState<T>> {
  return source.pipe(
    map((value): LoadState<T> => ({ status: 'ready', value })),
    catchError((error: unknown) =>
      of<LoadState<T>>(
        error instanceof HttpErrorResponse && error.status === HttpStatusCode.NotFound
          ? { status: 'missing' }
          : { status: 'error' },
      ),
    ),
    startWith<LoadState<T>>({ status: 'loading' }),
  );
}
