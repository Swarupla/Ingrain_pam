import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { CookieDeclinePopupComponent } from './cookie-decline-popup.component';

describe('CookieDeclinePopupComponent', () => {
  let component: CookieDeclinePopupComponent;
  let fixture: ComponentFixture<CookieDeclinePopupComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ CookieDeclinePopupComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CookieDeclinePopupComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
