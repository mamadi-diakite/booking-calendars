import { HttpErrorResponse } from '@angular/common/http';
import { Observable, of, throwError, toArray } from 'rxjs';
import { LoadState, withLoadState } from './load-state';

function states<T>(source: Observable<T>): LoadState<T>[] {
  let result: LoadState<T>[] = [];
  withLoadState(source)
    .pipe(toArray())
    .subscribe((values) => (result = values));
  return result;
}

describe('withLoadState', () => {
  it('starts loading, then gives the value', () => {
    expect(states(of(42))).toEqual([{ status: 'loading' }, { status: 'ready', value: 42 }]);
  });

  it('reports a missing resource on 404', () => {
    const notFound = new HttpErrorResponse({ status: 404 });

    expect(states(throwError(() => notFound))).toEqual([
      { status: 'loading' },
      { status: 'missing' },
    ]);
  });

  it('reports any other failure as an error', () => {
    const unavailable = new HttpErrorResponse({ status: 503 });

    expect(states(throwError(() => unavailable))).toEqual([
      { status: 'loading' },
      { status: 'error' },
    ]);
    expect(states(throwError(() => new Error('offline')))).toEqual([
      { status: 'loading' },
      { status: 'error' },
    ]);
  });
});
