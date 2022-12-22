import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { SessionTimeoutPopupComponent } from './session-timeout-popup.component';

describe('SessionTimeoutPopupComponent', () => {
  let component: SessionTimeoutPopupComponent;
  let fixture: ComponentFixture<SessionTimeoutPopupComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ SessionTimeoutPopupComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SessionTimeoutPopupComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
