import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { MyUtilitiesComponent } from './my-utilities.component';

describe('MyUtilitiesComponent', () => {
  let component: MyUtilitiesComponent;
  let fixture: ComponentFixture<MyUtilitiesComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ MyUtilitiesComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(MyUtilitiesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
