import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { MissingValuesComponent } from './missing-values.component';

describe('MissingValuesComponent', () => {
  let component: MissingValuesComponent;
  let fixture: ComponentFixture<MissingValuesComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ MissingValuesComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(MissingValuesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
